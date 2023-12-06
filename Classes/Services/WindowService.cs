using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace RePlays.Services {
    public static class WindowService {
#if WINDOWS
        static readonly ManagementEventWatcher pCreationWatcher = new(new EventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));
        static readonly ManagementEventWatcher pDeletionWatcher = new(new EventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));

        static MessageWindow messageWindow;
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        static WinEventProc winActiveDele, winResizeDele; // Keep win event delegates alive as long as class is alive (if you dont do this, gc will clean up)
        static nint winActiveHook, winResizeHook;

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, Int32 dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        static extern bool GetProcessImageFileName(IntPtr hprocess, StringBuilder lpExeName, out int size);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);
#else
        private static Thread X11WindowWatcher;
        static IntPtr X11Display;
        static IntPtr X11RootWindow;
        static Dictionary<nint, X11Window> x11Windows = [];
        struct X11Window {
            public nint id;
            public string title;
            public string classname;
            public int pid;
        }
        const string Xlib = "libX11";

        [DllImport(Xlib, EntryPoint = "XOpenDisplay")]
        static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport(Xlib, EntryPoint = "XDefaultRootWindow")]
        static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport(Xlib, EntryPoint = "XQueryTree")]
        static extern int XQueryTree(IntPtr display, IntPtr window, out IntPtr root, out IntPtr parent, out IntPtr children, out uint nChildren);

        [DllImport(Xlib, EntryPoint = "XFree")]
        static extern int XFree(IntPtr data);

        [DllImport(Xlib, EntryPoint = "XFetchName", CharSet = CharSet.Ansi)]
        static extern int XFetchName(IntPtr display, IntPtr window, ref IntPtr windowName);

        [DllImport(Xlib, EntryPoint = "XGetWindowProperty")]
        static extern int XGetWindowProperty(IntPtr display, IntPtr window, IntPtr property, long offset, long length, bool delete, IntPtr req_type, out IntPtr actual_type, out int actual_format, out uint nitems, out uint bytes_after, out IntPtr prop);

        [DllImport(Xlib, EntryPoint = "XCloseDisplay")]
        static extern int XCloseDisplay(IntPtr display);

        // Xlib structures and functions for WM_CLASS
        [StructLayout(LayoutKind.Sequential)]
        struct XClassHint {
            public IntPtr res_name;
            public IntPtr res_class;
        }

        [DllImport(Xlib, EntryPoint = "XGetClassHint")]
        static extern int XGetClassHint(IntPtr display, IntPtr window, ref XClassHint classHint);

        // Xlib functions for _NET_WM_PID
        [DllImport(Xlib, EntryPoint = "XInternAtom")]
        static extern IntPtr XInternAtom(IntPtr display, string atomName, bool onlyIfExists);

        static readonly IntPtr XA_CARD = (IntPtr)6; // Atom type for CARDINAL
#endif
        private static Dictionary<string, string> drivePaths = [];
        public static bool IsStarted { get; internal set; }

        public static IntPtr GetWindowHandleByProcessId(int processId, bool lazy = false) {
            IntPtr handle;
            try {
                if (!lazy) handle = EnumerateProcessWindowHandles(processId).First();
                else handle = Process.GetProcessById(processId).MainWindowHandle;
            }
            catch (Exception e) {
                Logger.WriteLine($"There was an issue retrieving the window handle for process id [{processId}]: {e.Message}");
                return IntPtr.Zero;
            }
            return handle;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetClientRect(IntPtr hwnd, ref Rect lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect {
            public int Left, Top, Right, Bottom;

            public Rect(int _left, int _top, int _right, int _bottom) : this() {
                Left = _left;
                Top = _top;
                Right = _right;
                Bottom = _bottom;
            }

            public int GetWidth() {
                return this.Right - this.Left;
            }

            public int GetHeight() {
                return this.Bottom - this.Top;
            }
        }

        public static Rect GetWindowSize(IntPtr handle) {
            Rect rect = new(0, 0, 0, 0);
#if WINDOWS
            if (!GetClientRect(handle, ref rect)) {
                Logger.WriteLine("Issue using GetClientRect");
                return rect;
            }
#endif

            return rect;
        }

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, [In, Out] ref Rect rect);

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        // https://stackoverflow.com/a/55542400
        public static bool IsFullscreen(nint windowHandle) {
            MONITORINFOEX monitorInfo = new();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            GetMonitorInfo(MonitorFromWindow(windowHandle, 1), ref monitorInfo);

            if (windowHandle != GetDesktopWindow() && windowHandle != GetShellWindow()) {
                Rect windowRect = new();
                GetWindowRect(windowHandle, ref windowRect);

                bool result = windowRect.Left == monitorInfo.rcMonitor.Left
                    && windowRect.Right == monitorInfo.rcMonitor.Right
                    && windowRect.Top == monitorInfo.rcMonitor.Top
                    && windowRect.Bottom == monitorInfo.rcMonitor.Bottom;

                Logger.WriteLine($"Window handle fullscreen exclusive: {result}");
                return result;
            }
            Logger.WriteLine("Window handle not detected as fullscreen exclusive.");
            return false;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hmonitor, [In, Out] ref MONITORINFOEX info);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        public class MONITORINFOEX {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
            public Rect rcMonitor;
            public Rect rcWork;
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szDevice = new char[32];
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DisplayDevice {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        public static string GetMonitorId(string deviceName) {
            Logger.WriteLine($"Attempting to retrieve deviceId from {deviceName}");
            Dictionary<string, string> monitorIds = [];
            _ = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) => {
                MONITORINFOEX monitorInfo = new();
                monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
                try {
                    if (GetMonitorInfo(hMonitor, ref monitorInfo)) {
                        DisplayDevice d = new();
                        d.cb = Marshal.SizeOf(d);
                        string lpDevice = string.Join("", monitorInfo.szDevice).TrimEnd('\0');
                        EnumDisplayDevices(lpDevice, 0, ref d, 0x1);
                        monitorIds.Add(lpDevice, d.DeviceID);
                        Logger.WriteLine($"Found deviceId: {d.DeviceID} for {lpDevice}");
                    }
                }
                catch (Exception ex) {
                    Logger.WriteLine($"Error in retrieval of monitor information: {ex.Message}");
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            if (!monitorIds.TryGetValue(deviceName, out string monitorId)) {
                Logger.WriteLine($"Could not retrieve deviceId for {deviceName}");
                return "";
            }
            Logger.WriteLine($"Retrieved deviceId: {monitorId} from {deviceName}");
            return monitorId;
        }

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        // https://stackoverflow.com/a/67066227/8805016
        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn,
            IntPtr lParam);

        static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId) {
            var handles = new List<IntPtr>();

            try {
                foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                    EnumThreadWindows(thread.Id,
                        (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
            }
            catch (Exception) { }

            if (handles.Count == 0)
                handles.Add(IntPtr.Zero);

            return handles;
        }
        public static void Start() {
#if WINDOWS
            // Get device paths for mounted drive letters
            for (char letter = 'A'; letter <= 'Z'; letter++) {
                string driveLetter = letter + ":";
                StringBuilder s = new StringBuilder();
                if (QueryDosDevice(driveLetter, s, 1000)) {
                    drivePaths.Add(s.ToString(), driveLetter);
                }
            }

            // watch process creation/deletion events
            //pCreationWatcher.EventArrived += ...;
            //pCreationWatcher.Start();
            pDeletionWatcher.EventArrived += (object sender, EventArrivedEventArgs e) => {
                try {
                    if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription) {
                        uint processId = uint.Parse(instanceDescription.GetPropertyValue("Handle").ToString());
                        //var executablePath = instanceDescription.GetPropertyValue("ExecutablePath");
                        //var cmdLine = instanceDescription.GetPropertyValue("CommandLine");

                        WindowDeletion(0, processId);
                    }
                }
                catch (ManagementException) { }
            };
            pDeletionWatcher.Start();

            // watch window creation/deletion events
            messageWindow = new MessageWindow();
            IsStarted = true;

            // watch active foreground window events 
            winActiveDele = OnActiveForegroundEvent;
            winActiveHook = SetWinEventHook(3, 3, IntPtr.Zero, winActiveDele, 0, 0, 0);

            // watch window resize/move events 
            winResizeDele = OnWindowResizeMoveEvent;
            winResizeHook = SetWinEventHook(11, 11, IntPtr.Zero, winResizeDele, 0, 0, 0);
#else
            X11WindowWatcher = new(() => {
                X11Display = XOpenDisplay(IntPtr.Zero);
                if (X11Display == IntPtr.Zero) {
                    Logger.WriteLine("Failed to open display.");
                    return;
                }
                X11RootWindow = XDefaultRootWindow(X11Display);
                IsStarted = true;
                while (IsStarted) {
                    Dictionary<nint, X11Window> prevWindows = new(x11Windows);
                    x11Windows.Clear();
                    IntPtr clientListAtom = XInternAtom(X11Display, "_NET_CLIENT_LIST", true);
                    if (clientListAtom != IntPtr.Zero) {
                        if (XGetWindowProperty(X11Display, X11RootWindow, clientListAtom, 0L, ~0L, false, IntPtr.Zero, out nint actualType, out int actualFormat, out uint nItems, out _, out nint prop) == 0) {
                            if (actualFormat == 32 && nItems > 0) {
                                IntPtr[] windowList = new IntPtr[nItems];
                                Marshal.Copy(prop, windowList, 0, (int)nItems);
                                foreach (IntPtr window in windowList) {
                                    string windowName = GetWindowTitle(window);
                                    string windowClass = GetClassName(window);
                                    int windowPid = GetWindowPid(window);

                                    if (windowPid <= 0) continue;
                                    if (!prevWindows.ContainsKey(window) && !x11Windows.ContainsKey(window)) {
                                        DetectionService.WindowCreation(window, (uint)windowPid);
                                    }

                                    x11Windows[window] = new X11Window {
                                        id = window,
                                        title = windowName,
                                        classname = windowClass,
                                        pid = windowPid
                                    };
                                }

                                var destroyedWindows = prevWindows.Keys.Except(x11Windows.Keys);
                                foreach (var window in destroyedWindows) {
                                    string windowName = prevWindows[window].title;
                                    int windowPid = prevWindows[window].pid;
                                    DetectionService.WindowDeletion(window, (uint)windowPid);
                                }
                                XFree(prop);
                            }
                        }
                    }
                    Thread.Sleep(1);
                }
                XCloseDisplay(X11Display);
            });
            X11WindowWatcher.Start();
#endif
        }

        public static void Stop() {
#if WINDOWS
            if (messageWindow != null) {
                messageWindow.Close();
                messageWindow.Dispose();
            }
            pCreationWatcher.Stop();
            pDeletionWatcher.Stop();
            pCreationWatcher.Dispose();
            pDeletionWatcher.Dispose();
            UnhookWinEvent(winActiveHook);
            UnhookWinEvent(winResizeHook);
            winActiveDele = null;
            winResizeDele = null;
#else
            X11WindowWatcher.Join(); // Stop
#endif
        }
        public static string GetWindowTitle(IntPtr window) {
            string windowName = "";
            IntPtr windowNamePtr = IntPtr.Zero;
            IntPtr nameAtom = XInternAtom(X11Display, "_NET_WM_NAME", false);
            if (nameAtom != IntPtr.Zero) {
                if (XGetWindowProperty(X11Display, window, nameAtom, 0, 16384, false, IntPtr.Zero, out nint actualType, out int actualFormat, out uint nItems, out _, out nint prop) == 0) {
                    if (prop != IntPtr.Zero & nItems > 0) {
                        windowName = Marshal.PtrToStringAnsi(prop);
                        XFree(prop);
                    }
                }
            }

            if (windowName == "" && XFetchName(X11Display, window, ref windowNamePtr) != 0) {
                windowName = Marshal.PtrToStringAnsi(windowNamePtr);
                XFree(windowNamePtr);
            }

            return windowName == "" ? "Unknown" : windowName;
        }

        public static string GetClassName(IntPtr window) {
            XClassHint classHint = new();
            if (XGetClassHint(X11Display, window, ref classHint) != 0) {
                string res_name = Marshal.PtrToStringAnsi(classHint.res_name);
                string res_class = Marshal.PtrToStringAnsi(classHint.res_class);
                XFree(classHint.res_name);
                XFree(classHint.res_class);
                return $"{res_name}:{res_class}";
            }

            return "Unknown";
        }

        public static int GetWindowPid(IntPtr window) {
            IntPtr pidAtom = XInternAtom(X11Display, "_NET_WM_PID", false);

            if (pidAtom != IntPtr.Zero) {
                if (XGetWindowProperty(X11Display, window, pidAtom, 0, 1, false, IntPtr.Zero, out nint actualType, out int actualFormat, out uint nItems, out _, out nint prop) == 0) {
                    if (actualType == XA_CARD && actualFormat == 32 && nItems == 1) {
                        int pid = Marshal.ReadInt32(prop);
                        XFree(prop);
                        return pid;
                    }
                }
            }

            return -1;
        }

        public static void GetExecutablePathFromProcessId(uint processId, out string executablePath) {
            if (processId == 0) {
                executablePath = "";
                return;
            }

            Process process;
            string processName = "Unknown";
            try {
                process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch {
                executablePath = "";
                return;
            }

            try {
                // if this raises an exception, then that means this process is most likely being covered by anti-cheat (EAC)
                executablePath = Path.GetFullPath(process.MainModule.FileName);
            }
            catch (Exception ex) {
                // this method of using OpenProcess is reliable for getting the fullpath in case of anti-cheat
                IntPtr processHandle = OpenProcess(0x00000400 | 0x00000010, false, (int)processId);
                if (processHandle != IntPtr.Zero) {
                    StringBuilder stringBuilder = new(1024);
                    if (!GetProcessImageFileName(processHandle, stringBuilder, out int _)) {
                        Logger.WriteLine($"Failed to get process: [{processId}] full path. Error: {ex.Message}");
                        executablePath = "";
                    }
                    else {
                        string s = stringBuilder.ToString();
                        foreach (var drivePath in drivePaths) {
                            if (s.Contains(drivePath.Key)) s = s.Replace(drivePath.Key, drivePath.Value);
                        }
                        executablePath = s;
                    }
                    CloseHandle(processHandle);
                    return;
                }
                else {
                    Logger.WriteLine($"Failed to get process: [{processId}][{processName}] full path. Error: {ex.Message}");
                    executablePath = "";
                    return;
                }
            }
        }
#if !WINDOWS
        public static bool GetForegroundWindow(out int processId, out nint hwid) {
            processId = 0; hwid = 0;
            return false;
        }

        public static IntPtr GetWindowThreadProcessId(nint hwnd, out uint processId) {
            processId = 0;
            return IntPtr.Zero;
        }

        public static IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, Int32 dwProcessId) {
            return IntPtr.Zero;
        }

        public static bool CloseHandle(IntPtr hObject) {
            return false;
        }

        public static bool GetProcessImageFileName(IntPtr hprocess, StringBuilder lpExeName, out int size) {
            size = 0;
            return false;
        }
#else
        static void OnActiveForegroundEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            GetForegroundWindow(out int pid, out _);
            if (RecordingService.IsRecording) {
                if (pid == RecordingService.GetCurrentSession().Pid) RecordingService.GainedFocus();
                else if (RecordingService.GameInFocus) RecordingService.LostFocus();
                return;
            }
            else {
                WindowCreation(hwnd);
            }
        }

        static void OnWindowResizeMoveEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (RecordingService.IsRecording) {
                if (pid == RecordingService.GetCurrentSession().Pid && hwnd == RecordingService.GetCurrentSession().WindowHandle) {
                    var windowSize = BaseRecorder.GetWindowSize(hwnd);
                    Logger.WriteLine($"WindowResize: [{hwnd}][{windowSize.GetWidth()}x{windowSize.GetHeight()}]");
                }
                return;
            }
        }
        [DllImport("user32.dll", EntryPoint = "GetForegroundProcess")]
        private static extern IntPtr GetForegroundProcess();
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);
        public static bool GetForegroundWindow(out int processId, out nint hwid) {
            IntPtr handle = GetForegroundProcess();

            if (handle == IntPtr.Zero) {
                hwid = 0;
                processId = 0;
                return false;
            }
            else hwid = handle;

            if (GetWindowThreadProcessId(handle, out int id) == 0) {
                hwid = 0;
                processId = 0;
                return false;
            }
            else processId = id;

            return true;
        }
        // http://www.pinvoke.net/default.aspx/user32.getclassname
        [DllImport("user32.dll", EntryPoint = "GetClassName", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int _GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "GetWindowText")]
        static extern int _GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public static string GetWindowTitle(IntPtr hWnd) {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);

            if (_GetWindowText(hWnd, Buff, nChars) == 0)
                return "";

            return Buff.ToString();
        }

        public static string GetClassName(IntPtr handle) {
            StringBuilder className = new(256);
            _ = _GetClassName(handle, className, className.Capacity);
            return className.ToString();
        }
    }

    // https://stackoverflow.com/a/7033382
    class MessageWindow : Form {
        private readonly int msgNotify;
        public delegate void WindowHandleEvent(object sender, object data);

        public MessageWindow() {
            var accessHandle = this.Handle;

            // Hook on to the shell for window creation/delection events
            msgNotify = RegisterWindowMessage("SHELLHOOK");
            _ = RegisterShellHookWindow(this.Handle);
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ChangeToMessageOnlyWindow();
        }

        private void ChangeToMessageOnlyWindow() {
            IntPtr HWND_MESSAGE = new(-3);
            SetParent(this.Handle, HWND_MESSAGE);
        }

        protected override void WndProc(ref Message m) {
            if (DetectionService.IsStarted && m.Msg == msgNotify) {
                // Receive shell messages
                switch (m.WParam.ToInt32()) {
                    case 1:  // HSHELL_WINDOWCREATED
                    case 4:  // HSHELL_WINDOWACTIVATED
                    case 13: // HSHELL_WINDOWREPLACED 
                        DetectionService.WindowCreation(m.LParam);
                        break;
                    case 2: // HSHELL_WINDOWDESTROYED
                        DetectionService.WindowDeletion(m.LParam);
                        break;
                }
            }
            base.WndProc(ref m);
        }

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int RegisterShellHookWindow(IntPtr hWnd);

        [DllImport("user32", EntryPoint = "RegisterWindowMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int RegisterWindowMessage(string lpString);
#endif
    }
}
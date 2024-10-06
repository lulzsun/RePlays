#pragma warning disable CA1806
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
#if WINDOWS
using System.Windows.Forms;
using System.Management;
using System.Runtime.ConstrainedExecution;
using System.Security;
#else
using System.Threading;
#endif

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

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

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

        [DllImport("user32.dll", EntryPoint = "GetClassName", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int _GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "GetWindowText")]
        static extern int _GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetClientRect(IntPtr hwnd, ref Rect lpRect);

        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        private static extern IntPtr _GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
        static extern uint _GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, [In, Out] ref Rect rect);

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        // https://stackoverflow.com/a/67066227/8805016
        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hmonitor, [In, Out] MONITORINFOEX info);

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
            public Rect size;
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

        [DllImport(Xlib, EntryPoint = "XGetClassHint")]
        static extern int XGetClassHint(IntPtr display, IntPtr window, ref XClassHint classHint);

        [DllImport(Xlib, EntryPoint = "XInternAtom")]
        static extern IntPtr XInternAtom(IntPtr display, string atomName, bool onlyIfExists);

        static readonly IntPtr XA_CARD = (IntPtr)6; // Atom type for CARDINAL

        [DllImport(Xlib, EntryPoint = "XSetErrorHandler")]
        static extern int XSetErrorHandler(XErrorHandler handler);

        [DllImport(Xlib, EntryPoint = "XGetErrorText")]
        static extern int XGetErrorText(IntPtr display, byte code, IntPtr buffer_return, int length);

        [DllImport(Xlib, EntryPoint = "XGetGeometry")]
        static extern int XGetGeometry(IntPtr display, IntPtr drawable, out IntPtr root, out int x, out int y,
            out uint width, out uint height, out uint border_width, out uint depth);

        delegate int XErrorHandler(IntPtr display, ref XErrorEvent error_event);

        [StructLayout(LayoutKind.Sequential)]
        struct XErrorEvent {
            public int type;
            public IntPtr display;
            public IntPtr resourceid;
            public IntPtr serial;
            public byte error_code;
            public byte request_code;
            public byte minor_code;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct XClassHint {
            public IntPtr res_name;
            public IntPtr res_class;
        }
#endif
        private static Dictionary<string, string> drivePaths = [];
        public static bool IsStarted { get; internal set; }

        public static IntPtr GetWindowHandleByProcessId(int processId, bool lazy = false) {
            try {
#if WINDOWS
                if (!lazy)
                    return EnumerateProcessWindowHandles(processId).First();
#endif
                return Process.GetProcessById(processId).MainWindowHandle;
            }
            catch (Exception e) {
                Logger.WriteLine($"There was an issue retrieving the window handle for process id [{processId}]: {e.Message}");
                return IntPtr.Zero;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect(int left, int top, int right, int bottom) {
            public int Left = left, Top = top, Right = right, Bottom = bottom;

            public int GetWidth() {
                return Right - Left;
            }

            public int GetHeight() {
                return Bottom - Top;
            }

            public string GetSizeStr() {
                return GetWidth() + "x" + GetHeight();
            }
        }

        public static Rect GetWindowSize(IntPtr window) {
            Rect rect = new(0, 0, 0, 0);
#if WINDOWS
            if (!GetClientRect(window, ref rect)) {
                Logger.WriteLine("Issue using GetClientRect");
                return rect;
            }
#else
            if (XGetGeometry(X11Display, window, out _, out _, out _, out uint width, out uint height, out _, out _) != 0) {
                return new Rect(0, 0, (int)width, (int)height);
            }
#endif
            return rect;
        }

        // https://stackoverflow.com/a/55542400
        public static bool IsFullscreen(nint windowHandle) {
#if WINDOWS
            MONITORINFOEX monitorInfo = new();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            GetMonitorInfo(MonitorFromWindow(windowHandle, 1), monitorInfo);

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
#endif
            return false;
        }

        public static void Start() {
            Logger.WriteLine("WindowService starting...");
#if WINDOWS
            // Get device paths for mounted drive letters
            foreach (var drive in DriveInfo.GetDrives()) {
                if (drive.IsReady) {
                    var driveLetter = drive.Name.TrimEnd('\\');
                    var devicePath = new StringBuilder(260); //MAX_PATH
                    if (QueryDosDevice(driveLetter, devicePath, devicePath.Capacity)) {
                        drivePaths.Add(devicePath.ToString(), driveLetter);
                    }
                }
            }

            // watch process creation/deletion events
            //pCreationWatcher.EventArrived += ...;
            //pCreationWatcher.Start();
            pDeletionWatcher.EventArrived += (object sender, EventArrivedEventArgs e) => {
                try {
                    if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription) {
                        int processId = int.Parse(instanceDescription.GetPropertyValue("Handle").ToString());
                        //var executablePath = instanceDescription.GetPropertyValue("ExecutablePath");
                        //var cmdLineArgs = instanceDescription.GetPropertyValue("CommandLine");

                        DetectionService.WindowDeletion(0, processId);
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

                XSetErrorHandler(
                    (IntPtr display, ref XErrorEvent error_event) => {
                        IntPtr errorMessage = IntPtr.Zero;
                        XGetErrorText(display, error_event.error_code, IntPtr.Zero, 0);
                        errorMessage = Marshal.AllocHGlobal(256);
                        XGetErrorText(display, error_event.error_code, errorMessage, 256);

                        string errorString = Marshal.PtrToStringAnsi(errorMessage);
                        Logger.WriteLine($"Xlib Error: {errorString}");

                        Marshal.FreeHGlobal(errorMessage);
                        return 0;
                    }
                );

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
                                XFree(prop);
                                foreach (IntPtr window in windowList) {
                                    string windowName = GetWindowTitle(window);

                                    if (string.IsNullOrEmpty(windowName)) continue;

                                    string windowClass = GetClassName(window);
                                    var windowSize = GetWindowSize(window);
                                    int windowPid = GetWindowPid(window);

                                    if (windowPid <= 0) continue;

                                    bool hasPrev = prevWindows.TryGetValue(window, out X11Window prev);
                                    bool hasCurr = x11Windows.TryGetValue(window, out X11Window curr);

                                    if (!hasPrev && !hasCurr) {
                                        DetectionService.WindowCreation(window, windowPid);
                                    }

                                    x11Windows[window] = new X11Window {
                                        id = window,
                                        title = windowName,
                                        classname = windowClass,
                                        pid = windowPid,
                                        size = windowSize
                                    };

                                    hasCurr = x11Windows.TryGetValue(window, out X11Window newCurr);
                                    curr = newCurr;

                                    if (hasPrev && hasCurr && prev.size.GetSizeStr() != curr.size.GetSizeStr()) {
                                        DetectionService.WindowCreation(window, windowPid);
                                    }
                                }

                                if (RecordingService.IsRecording && !RecordingService.IsStopping) {
                                    var session = RecordingService.GetCurrentSession();
                                    if (!x11Windows.ContainsKey(session.WindowHandle)) {
                                        DetectionService.WindowDeletion(session.WindowHandle, session.Pid);
                                    }
                                }
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
            Logger.WriteLine("WindowService stopping...");
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
#if !WINDOWS
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
#else
            StringBuilder buffer = new(256);

            if (_GetWindowText(window, buffer, 256) == 0)
                windowName = "";
            else
                windowName = buffer.ToString();
#endif
            return windowName;
        }

        public static string GetClassName(IntPtr window) {
#if !WINDOWS
            XClassHint classHint = new();
            if (XGetClassHint(X11Display, window, ref classHint) != 0) {
                string res_name = Marshal.PtrToStringAnsi(classHint.res_name);
                string res_class = Marshal.PtrToStringAnsi(classHint.res_class);
                XFree(classHint.res_name);
                XFree(classHint.res_class);
                return $"{res_name}"; //xcompositor-input only uses res_name: https://github.com/obsproject/obs-studio/blob/e27b013d4754e0e81119ab237ffedce8fcebcbbf/plugins/linux-capture/xcomposite-input.c#L207
            }
            return "";
#else
            StringBuilder className = new(256);
            _ = _GetClassName(window, className, className.Capacity);
            return className.ToString();
#endif
        }

        public static int GetWindowPid(IntPtr window) {
#if !WINDOWS
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
#endif
            return -1;
        }

        public static uint GetWindowThreadProcessId(nint hwnd, out int processId) {
#if !WINDOWS
            processId = 0;
            return 0;
#else
            return _GetWindowThreadProcessId(hwnd, out processId);
#endif
        }

        public static void GetExecutablePathFromProcessId(int processId, out string executablePath) {
            if (processId == 0) {
                executablePath = "";
                return;
            }

            Process process;
            string processName = "Unknown";
            try {
                process = Process.GetProcessById(processId);
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
                        Logger.WriteLine($"Failed to get process: [{processId}] full path. Unable to get process image filename.");
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
                    // this is probably a permissions issue (requires admin priv)
                    if (ex.GetType() != typeof(System.ComponentModel.Win32Exception))
                        Logger.WriteLine($"Failed to get process: [{processId}][{processName}] full path. Error: {ex.GetType()}:{ex.Message}");
                    executablePath = "";
                    return;
                }
            }
        }

        public static List<IntPtr> GetTopLevelWindows() {
            List<IntPtr> windowHandles = new();
#if WINDOWS
            EnumWindows((hWnd, lParam) => {
                GCHandle handle = GCHandle.FromIntPtr(lParam);
                List<IntPtr> handles = (List<IntPtr>)handle.Target;
                if (!string.IsNullOrEmpty(GetWindowTitle(hWnd))) {
                    handles.Add(hWnd);
                    return true;
                }
                return false;
            }, GCHandle.ToIntPtr(GCHandle.Alloc(windowHandles)));
#endif
            return windowHandles;
        }

#if !WINDOWS
        public static bool GetForegroundWindow(out int processId, out nint hwid) {
            processId = 0; hwid = 0;
            return false;
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
        public static string GetMonitorId(string deviceName) {
            Logger.WriteLine($"Attempting to retrieve deviceId from {deviceName}");
            Dictionary<string, string> monitorIds = [];
            _ = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) => {
                MONITORINFOEX monitorInfo = new();
                monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
                try {
                    if (GetMonitorInfo(hMonitor, monitorInfo)) {
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

        static List<IntPtr> EnumerateProcessWindowHandles(int processId) {
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

        static void OnActiveForegroundEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            GetForegroundWindow(out int pid, out _);
            if (RecordingService.IsRecording) {
                if (pid == RecordingService.GetCurrentSession().Pid) RecordingService.GainedFocus();
                else if (RecordingService.GameInFocus) RecordingService.LostFocus();
                return;
            }
            else {
                DetectionService.WindowCreation(hwnd);
            }
        }

        static void OnWindowResizeMoveEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            GetWindowThreadProcessId(hwnd, out int pid);
            if (RecordingService.IsRecording) {
                if (pid == RecordingService.GetCurrentSession().Pid && hwnd == RecordingService.GetCurrentSession().WindowHandle) {
                    var windowSize = GetWindowSize(hwnd);
                    Logger.WriteLine($"WindowResize: [{hwnd}][{windowSize.GetWidth()}x{windowSize.GetHeight()}]");
                }
                return;
            }
        }

        public static bool GetForegroundWindow(out int processId, out nint hwid) {
            IntPtr handle = _GetForegroundWindow();

            if (handle == IntPtr.Zero) {
                hwid = 0;
                processId = 0;
                return false;
            }
            else hwid = handle;

            if (_GetWindowThreadProcessId(handle, out int id) == 0) {
                hwid = 0;
                processId = 0;
                return false;
            }
            else processId = id;

            return true;
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
            if (WindowService.IsStarted && m.Msg == msgNotify) {
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
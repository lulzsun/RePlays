using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RePlays.Recorders {
    public abstract class BaseRecorder {
        public abstract void Start();
        public abstract Task<bool> StartRecording();
        public abstract Task<bool> StopRecording();
        public abstract void LostFocus();
        public abstract void GainedFocus();

        public IntPtr GetWindowHandleByProcessId(int processId, bool lazy = false) {
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

            if (!GetClientRect(handle, ref rect)) {
                Logger.WriteLine("Issue using GetClientRect");
                return rect;
            }

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

        // http://www.pinvoke.net/default.aspx/user32.getclassname
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public string GetWindowTitle(IntPtr hWnd) {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);

            if (GetWindowText(hWnd, Buff, nChars) == 0)
                return "";

            return Buff.ToString();
        }

        public string GetClassName(IntPtr handle) {
            StringBuilder className = new(256);
            _ = GetClassName(handle, className, className.Capacity);
            return className.ToString();
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
            Dictionary<string, string> monitorIds = new();
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
    }
}
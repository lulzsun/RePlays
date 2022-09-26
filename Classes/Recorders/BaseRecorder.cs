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

        public IntPtr LazyGetWindowHandleByProcessId(int processId) {
            IntPtr handle = IntPtr.Zero;
            try {
                handle = Process.GetProcessById(processId).MainWindowHandle;
            }
            catch (Exception e) {
                Logger.WriteLine($"There was an issue retrieving the window handle for process id [{processId}]: {e.Message}");
            }
            return handle;
        }

        public IntPtr GetWindowHandleByProcessId(int processId) {
            IntPtr handle = IntPtr.Zero;
            try {
                handle = EnumerateProcessWindowHandles(processId).First();
            }
            catch (Exception e) {
                Logger.WriteLine($"There was an issue retrieving the window handle for process id [{processId}]: {e.Message}");
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

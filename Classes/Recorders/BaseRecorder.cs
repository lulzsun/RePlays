using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace RePlays.Recorders {
    public abstract class BaseRecorder {
        readonly ManagementEventWatcher pCreationWatcher = new(new EventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));
        readonly ManagementEventWatcher pDeletionWatcher = new(new EventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));

        // https://stackoverflow.com/a/14407610/8805016
        WinEventDelegate dele = null;
        IntPtr winActiveHook = IntPtr.Zero;

        public virtual void Start() {
            // watch process creation/deletion events
            pCreationWatcher.EventArrived += ProcessCreation_EventArrived;
            pDeletionWatcher.EventArrived += ProcessDeletion_EventArrived;
            pCreationWatcher.Start();
            pDeletionWatcher.Start();

            // watch active foreground window changes 
            dele = new WinEventDelegate(WhenActiveForegroundChanges);
            winActiveHook = SetWinEventHook(3, 3, IntPtr.Zero, dele, 0, 0, 0);
        }

        public virtual void Stop() {
            pCreationWatcher.Stop();
            pDeletionWatcher.Stop();
            pCreationWatcher.Dispose();
            pDeletionWatcher.Dispose();

            UnhookWinEvent(winActiveHook);
            dele = null;
        }

        public abstract Task<bool> StartRecording();
        public abstract Task<bool> StopRecording();

        public void ProcessCreation_EventArrived(object sender, EventArrivedEventArgs e) {
            if (RecordingService.IsRecording) return;

            try {
                if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription) {
                    int processId = Int32.Parse(instanceDescription.GetPropertyValue("Handle").ToString());
                    var executablePath = instanceDescription.GetPropertyValue("ExecutablePath");
                    var cmdLine = instanceDescription.GetPropertyValue("CommandLine"); // may or may not be useful in the future

                    if (executablePath != null) {
                        if (executablePath.ToString().ToLower().StartsWith(@"c:\windows\")) {   // if this program is starting from here,
                            return;                                                             // we can assume it is not a game
                        }
                        AutoDetectGame(processId, executablePath.ToString());
                    }
                    else if (processId != 0)AutoDetectGame(processId);
                }
            }
            catch (ManagementException) { }

            e.NewEvent.Dispose();
        }

        public void ProcessDeletion_EventArrived(object sender, EventArrivedEventArgs e) {
            if (!RecordingService.IsRecording) return;

            try {
                if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription) {
                    int processId = Int32.Parse(instanceDescription.GetPropertyValue("Handle").ToString());

                    if (processId != 0) {
                        if (RecordingService.GetCurrentSession().Pid == processId)
                            RecordingService.StopRecording();
                    }
                }
            }
            catch (ManagementException) { }

            e.NewEvent.Dispose();
        }

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public int GetForegroundProcessId() {
            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
                return 0;
            if (GetWindowThreadProcessId(handle, out int processId) == 0)
                return 0;
            // string title = GetWindowTitle(handle);

            return processId;
        }

        public IntPtr GetWindowHandleByProcessId(int processId) {
            IntPtr handle = IntPtr.Zero;
            try {
                handle = EnumerateProcessWindowHandles(processId).First();
            }
            catch (Exception e) {
                Logger.WriteLine(String.Format("There was an issue retrieving the window handle for process id [{0}]: {1}", processId, e.Message));
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

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

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

        // this event should occur when the active foreground changes
        public void WhenActiveForegroundChanges(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            if (RecordingService.IsRecording) return;

            AutoDetectGame(GetForegroundProcessId());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, Int32 dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, uint nSize);

        /// <summary>
        /// <para>Checks to see if the process:</para>
        /// <para>1. contains in the game detection list (whitelist)</para>
        /// <para>2. does NOT contain in nongame detection list (blacklist)</para>
        /// <para>3. contains any graphics dll modules (directx, opengl)</para>
        /// <para>If 2 and 3 are true, we will also assume it is a "game"</para>
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="executablePath">Full path to executable, if possible</param>
        public void AutoDetectGame(int processId, string executablePath = null, bool autoRecord = true) {
            bool isGame = false, isNonGame = false;
            string exeFile = executablePath;
            string modules = "";

            Process[] processlist = Process.GetProcesses();
            using Process process = processlist.FirstOrDefault(pr => pr.Id == processId);

            if (process != null) {
                if (exeFile == null) {
                    exeFile = process.ProcessName + ".exe";
                }

                isNonGame = DetectionService.IsMatchedNonGame(exeFile);
                if (isNonGame) {
                    return;
                }

                IntPtr processHandle = OpenProcess(0x0400 | 0x0010, //PROCESS_QUERY_INFORMATION | PROCESS_VM_READ
                    false, process.Id);

                if (processHandle != IntPtr.Zero) {
                    StringBuilder stringBuilder = new(1024);
                    if (GetModuleFileNameEx(processHandle, IntPtr.Zero, stringBuilder, (uint)stringBuilder.Capacity) == 0) {
                        Logger.WriteLine(string.Format("Failed to get process [{0}] [{1}] full path.", process.Id, exeFile));
                    }
                    else {
                        exeFile = stringBuilder.ToString();
                    }
                    CloseHandle(processHandle);
                }
                else {
                    Logger.WriteLine(string.Format("Failed to open process [{0}] [{1}].", process.Id, exeFile));
                }
            }

            string gameTitle = DetectionService.GetGameTitle(exeFile);

            if (!autoRecord) {
                // This is a manual record event so lets just yolo it and assume user knows best
                RecordingService.SetCurrentSession(processId, gameTitle);
                RecordingService.GetCurrentSession().Exe = exeFile;
                return;
            }

            isGame = DetectionService.IsMatchedGame(exeFile);

            if (!isGame && process != null) {
                Logger.WriteLine(string.Format("Process [{0}] isn't in the game detection list, checking if it might be a game", Path.GetFileName(exeFile)));
                try {
                    foreach (ProcessModule module in process.Modules) {
                        if (module == null) continue;

                        var name = module.ModuleName.ToLower();
                        modules += ", " + module.ModuleName;

                        // this could cause false positives, but it should be ok for most applications
                        if (name.StartsWith("explorerframe") || name.StartsWith("desktop-notifications") || name.StartsWith("squirrel")) {
                            isGame = false;
                            break;
                        }

                        if (name.StartsWith("d3d") || name.StartsWith("opengl")) {
                            isGame = true;
                            Logger.WriteLine(string.Format("This process [{0}]:[{1}] : [{2}], appears to be a game.", processId, name, Path.GetFileName(exeFile)));
                        }
                        module.Dispose();
                    }
                }
                catch (Exception e) { // sometimes, the process locks us out from reading and throws exception (anticheat functionality?)
                    Logger.WriteLine(string.Format("Failed to view all ProcessModules for [{0}{1}] isGame: {2} isNonGame: {3}, reason: {4}", Path.GetFileName(exeFile), modules, isGame, isNonGame, e.Message));
                }
            }

            if (isGame) {
                if (!EnumerateProcessWindowHandles(processId).Any()) return;

                RecordingService.SetCurrentSession(processId, gameTitle);
                RecordingService.GetCurrentSession().Exe = exeFile;

                Logger.WriteLine(string.Format("This process [{0}] is a recordable game [{1}{2}], prepared to record", processId, Path.GetFileName(exeFile), modules));

                if (autoRecord && SettingsService.Settings.captureSettings.recordingMode == "automatic")
                    RecordingService.StartRecording();
            }
        }
    }
}

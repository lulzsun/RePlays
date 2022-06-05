using RePlays.Services;
using RePlays.Utils;
using static RePlays.Utils.Functions;
using System.Management;
using System.Diagnostics;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace RePlays.Recorders {
    public class LibObsRecorder : BaseRecorder {
        public bool Connected { get; private set; }

        ManagementEventWatcher pCreationWatcher = new(new EventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));
        ManagementEventWatcher pDeletionWatcher = new(new EventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));

        public override void Start() {
            if (Connected) return;

            pCreationWatcher.EventArrived += ProcessCreation_EventArrived;
            pDeletionWatcher.EventArrived += ProcessDeletion_EventArrived;
            pCreationWatcher.Start();
            pDeletionWatcher.Start();

            dele = new WinEventDelegate(WinEventProc);
            IntPtr m_hhook = SetWinEventHook(3, 3, IntPtr.Zero, dele, 0, 0, 0);

            Connected = true;
            Logger.WriteLine("Successfully started LibObs!");
        }

        // https://stackoverflow.com/a/14407610/8805016
        WinEventDelegate dele = null;

        public override void Stop() {
            throw new System.NotImplementedException();
        }

        public override void StartRecording() {
            var session = RecordingService.GetCurrentSession();
            Logger.WriteLine(string.Format("LibObs started recording {0} {1}", session.Pid, session.GameTitle));
        }

        public override void StopRecording() {
            var session = RecordingService.GetCurrentSession();
            Logger.WriteLine(string.Format("LibObs stopped recording {0} {1}", session.Pid, session.GameTitle));
        }

        public void ProcessCreation_EventArrived(object sender, EventArrivedEventArgs e) {
            if (RecordingService.IsRecording) return;

            try {
                if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription) {
                    int processId = Int32.Parse(instanceDescription.GetPropertyValue("Handle").ToString());
                    var executablePath = instanceDescription.GetPropertyValue("ExecutablePath");
                    var cmdLine = instanceDescription.GetPropertyValue("CommandLine"); // may or may not be useful in the future

                    if (executablePath != null && !String.IsNullOrEmpty(executablePath.ToString())) {

                        bool isGame = DetectionService.IsMatchedGame(executablePath.ToString());
                        bool isNonGame = DetectionService.IsMatchedNonGame(executablePath.ToString());

                        if (isGame && !isNonGame) {
                            Logger.WriteLine(string.Format("This process [{0}] is a recordable game [{1}], prepared to record", processId, Path.GetFileName(executablePath.ToString())));

                            string gameTitle = DetectionService.GetGameTitle(executablePath.ToString());
                            RecordingService.SetCurrentSession(processId, gameTitle);

                            if (SettingsService.Settings.captureSettings.recordingMode == "automatic")
                                RecordingService.StartRecording();
                        }
                        else if (!isGame && !isNonGame) {
                            Logger.WriteLine(string.Format("This process [{0}] is an unknown application [{1}], lets guess if it is a game", processId, Path.GetFileName(executablePath.ToString())));

                            RecordingService.SetCurrentSession(0, DetectionService.GetGameTitle(executablePath.ToString(), true));
                            AutoDetectGame(processId);
                        }
                        else {
                            Logger.WriteLine(string.Format("This process [{0}] is a non-game [{1}]", processId, Path.GetFileName(executablePath.ToString())));
                        }
                    }
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
                    var executablePath = instanceDescription.GetPropertyValue("ExecutablePath");

                    if (executablePath != null) {
                        if (RecordingService.GetCurrentSession().Pid == processId)
                            RecordingService.StopRecording();
                    }
                }
            }
            catch (ManagementException) { }

            e.NewEvent.Dispose();
        }

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            if (RecordingService.IsRecording) return;

            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
                return;
            if (GetWindowThreadProcessId(handle, out int processId) == 0)
                return;

            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);

            if (GetWindowText(handle, Buff, nChars) == 0)
                return;

            string title = Buff.ToString();

            AutoDetectGame(processId);
        }

        /// <summary>
        /// Checks to see if the process contains any graphics dll modules (directx, opengl). If it does, we will assume it is a "game".
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="executablePath">Full path to executable, if possible</param>
        public void AutoDetectGame(int processId, string executablePath = null) {
            bool isGame = false;
            string exeFile = executablePath;

            try {
                using (Process process = Process.GetProcessById(processId)) {
                    if (process != null) {
                        string modules = "";

                        if (exeFile == null)
                            exeFile = process.ProcessName + ".exe";

                        try {
                            foreach (ProcessModule module in process.Modules) {
                                if (module == null) continue;

                                var name = module.ModuleName.ToLower();

                                if (name.StartsWith("d3d") || name.StartsWith("opengl")) {
                                    modules += ", " + module.ModuleName;
                                    isGame = true;
                                }
                            }
                        }
                        catch (Exception) {
                            if (DetectionService.IsMatchedGame(exeFile)) {
                                isGame = true;
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            if (isGame && !DetectionService.IsMatchedNonGame(exeFile)) {
                string gameTitle = DetectionService.GetGameTitle(exeFile);
                RecordingService.SetCurrentSession(processId, gameTitle);

                Logger.WriteLine(string.Format("This process [{0}] is a recordable game [{1}], prepared to record", processId, Path.GetFileName(exeFile)));

                if (SettingsService.Settings.captureSettings.recordingMode == "automatic")
                    RecordingService.StartRecording();
            }
        }
    }
}
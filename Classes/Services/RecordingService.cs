using RePlays.Recorders;
using RePlays.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using static RePlays.Utils.Functions;
using Timer = System.Timers.Timer;


namespace RePlays.Services {
    public static class RecordingService {
        public static BaseRecorder ActiveRecorder;

        private static Timer recordingTimer = new Timer(100);
        public static DateTime startTime;
        public static double lastVideoDuration = 0;
        private static Session currentSession = new(0, 0, "Game Unknown");
        public static bool IsStopping { get; internal set; }
        public static bool IsRecording { get; internal set; }
        public static bool IsPaused { get; internal set; }
        private static bool IsPreRecording { get; set; }
        private static bool IsRestarting { get; set; }
        public static bool GameInFocus { get; set; }

        const int retryInterval = 2000; // 2 second
        const int maxRetryAttempts = 20; // 30 retries

        public class Session {
            public int Pid { get; internal set; }
            public nint WindowHandle { get; internal set; }
            public string GameTitle { get; internal set; }
            public string Exe { get; internal set; }
            public bool ForceDisplayCapture { get; internal set; }
            public string videoSavePath { get; internal set; }

            public Session(int _Pid, nint _WindowHandle, string _GameTitle, string _Exe = null, bool _ForceDisplayCapture = false) {
                Pid = _Pid;
                WindowHandle = _WindowHandle;
                GameTitle = _GameTitle;
                Exe = _Exe;
                ForceDisplayCapture = _ForceDisplayCapture;
            }
        }

        public static async void Start(Type type) {
            Logger.WriteLine("RecordingService starting...");
            DetectionService.Start();
            if (type == typeof(PlaysLTCRecorder)) {
                ActiveRecorder = new PlaysLTCRecorder();
                ActiveRecorder.Start();
                return;
            }

            ActiveRecorder = new LibObsRecorder();
            Logger.WriteLine("Creating a new ActiveRecorder");
            await Task.Run(() => ActiveRecorder.Start());
            //Update user settings
            WebMessage.SendMessage(GetUserSettings());
            await Task.Run(() => DetectionService.CheckTopLevelWindows());
        }

        public static void SetCurrentSession(int _Pid, nint _WindowHandle, string _GameTitle, string exeFile, bool forceDisplayCapture = false) {
            currentSession = new Session(_Pid, _WindowHandle, _GameTitle, exeFile, forceDisplayCapture);
        }

        public static Session GetCurrentSession() {
            return currentSession;
        }

        private static async Task<bool> GetGoodWindowHandle() {
            int retryAttempt = 0;
            while ((DetectionService.HasBadWordInClassName(currentSession.WindowHandle) || currentSession.WindowHandle == IntPtr.Zero) && retryAttempt < maxRetryAttempts) {
                Logger.WriteLine($"Waiting to retrieve process handle... retry attempt #{retryAttempt}");
                await Task.Delay(retryInterval);
                retryAttempt++;
                // alternate on retry attempts, one or the other might get us a better handle
                currentSession.WindowHandle = WindowService.GetWindowHandleByProcessId(currentSession.Pid, retryAttempt % 2 == 1);
            }
            if (retryAttempt >= maxRetryAttempts) {
                return false;
            }
            return true;
        }

        private static bool HandleManualRecord() {
            if (WindowService.GetForegroundWindow(out int processId, out nint hwid)) {
                if (processId != 0 || hwid != 0) {
                    WindowService.GetExecutablePathFromProcessId(processId, out string executablePath);
                    DetectionService.AutoDetectGame(processId, executablePath, hwid, autoRecord: false);
                    return true;
                }
                return false;
            }
            return false;
        }

        private static bool SetSessionDetails() {
            string dir = Path.Join(GetPlaysFolder(), "/" + MakeValidFolderNameSimple(currentSession.GameTitle) + "/");
            try {
                Directory.CreateDirectory(dir);
            }
            catch (Exception e) {
                WebMessage.DisplayModal(string.Format("Unable to create folder {0}. Do you have permission to create it?", dir), "Recording Error", "warning");
                Logger.WriteLine(e.ToString());
                return false;
            }
            string videoNameTimeStamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");

            FileFormat currentFileFormat = SettingsService.Settings.captureSettings.fileFormat ?? (new FileFormat("mp4", "MP4 (.mp4)", true));
            Logger.WriteLine($"Output file format: " + currentFileFormat.ToString());
            currentSession.videoSavePath = Path.Join(dir, videoNameTimeStamp + "-ses." + currentFileFormat.GetFileExtension());
            return true;
        }

        //[STAThread]
        public static async void StartRecording(bool manual) {
            if (IsRecording || IsPreRecording) {
                Logger.WriteLine($"Cannot start recording, already recording [{currentSession.Pid}][{currentSession.GameTitle}]");
                return;
            }

            IsPreRecording = true;
            bool result = true;

            if (manual) result = HandleManualRecord();
            if (result) result = await GetGoodWindowHandle();
            if (result) result = SetSessionDetails();
            if (result) result = await ActiveRecorder.StartRecording();
            Logger.WriteLine("Start Success: " + result);
            Logger.WriteLine("Still allowed to record: " + (!IsRecording && result));
            if (!IsRecording && result) {
                Logger.WriteLine("Current Session PID: " + currentSession.Pid);

                startTime = DateTime.Now;
                recordingTimer.Elapsed += OnTimedEvent;
                recordingTimer.Start();

                Logger.WriteLine($"Start Recording: {currentSession.Pid}, {currentSession.GameTitle}");
                IsRecording = true;
                IsPreRecording = false;
                GameInFocus = true;

                if (SettingsService.Settings.captureSettings.useRecordingStartSound) {
                    Functions.PlaySound(Functions.GetResourcesFolder() + "start_recording.wav");
                }
            }
            if (!result) {
                // recorder failed to start properly so lets restart the currentSession Pid
                currentSession.Pid = 0;
            }
            IsPreRecording = false;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e) {
            if (!IsStopping) {
                WebMessage.DisplayToast("Recording", currentSession.GameTitle, "", "none", GetTotalRecordingTimeInSeconds());
            }
        }

        public static async void StopRecording(bool user = false) {
            if (!IsRecording) {
                Logger.WriteLine($"Cannot stop recording, no recording in progress");
                return;
            }
            IsStopping = true;

            bool error = await ActiveRecorder.StopRecording();

            if (error) {
                Logger.WriteLine("Warning: ActiveRecorder did not successfully stopped recording!");
            }

            if (user) {
                Logger.WriteLine("User-initiated stop recording.");
            }

            if (IsRecording || user || error) {
                recordingTimer.Elapsed -= OnTimedEvent;
                recordingTimer.Stop();
                Logger.WriteLine($"Stop Recording: {currentSession.Pid}, {currentSession.GameTitle}");
                currentSession.Pid = 0;
                WebMessage.DestroyToast("Recording");
                IsRecording = false;
                IsStopping = false;
                StorageService.ManageStorage();
            }
        }

        public static async void RestartRecording() {
            if (!IsRecording || IsRestarting) return;
            IsRestarting = true;

            bool stopResult = await ActiveRecorder.StopRecording();
            bool newSession = SetSessionDetails();
            bool startResult = false;
            if (newSession) startResult = await ActiveRecorder.StartRecording();

            if (stopResult && startResult) {
                Logger.WriteLine("Recording restart successful");
            }
            else {
                Logger.WriteLine($"Issue trying to restart recording. Could start {stopResult}, could stop {startResult}");
                IsRecording = false;
            }
            IsRestarting = false;
        }

        public static void LostFocus() {
            GameInFocus = false;
            ActiveRecorder.LostFocus();
        }

        public static void GainedFocus() {
            GameInFocus = true;
            ActiveRecorder.GainedFocus();
        }

        public static int GetTotalRecordingTimeInSeconds() {
            return (int)(DateTime.Now - startTime).TotalSeconds;
        }
        public static double GetTotalRecordingTimeInSecondsWithDecimals(DateTime? dateTime = null) {
            if (dateTime == null) dateTime = DateTime.Now;

            return (dateTime.Value - startTime).TotalMilliseconds / 1000;
        }
    }
}
using RePlays.Recorders;
using RePlays.Utils;
using System.Timers;
using System;
using System.Threading.Tasks;
using RePlays.Classes.Services;
using System.Windows.Forms;
using System.Diagnostics;

namespace RePlays.Services {
    public static class RecordingService {
        public static BaseRecorder ActiveRecorder = null;

        private static System.Timers.Timer recordingTimer = new System.Timers.Timer(1000);
        public static int recordingElapsed = 0;
        public static double lastVideoDuration = 0;
        private static Session currentSession = new Session(0, "Game Unknown");
        public static bool IsRecording { get; internal set; }
        private static bool IsPreRecording { get; set; }

        public class Session {
            public int Pid { get; internal set; }
            public string GameTitle { get; internal set; }
            public string Exe { get; internal set; }
            public Session(int _Pid, string _GameTitle, string _Exe=null) {
                Pid = _Pid;
                GameTitle = _GameTitle;
                Exe = _Exe;
            }
        }

        public async static void Start(Type type) {

            Logger.WriteLine("RecordingService starting...");
            DetectionService.Start();
            if (ActiveRecorder == null) {
                if (type == typeof(PlaysLTCRecorder)) {
                    ActiveRecorder = new PlaysLTCRecorder();
                    ActiveRecorder.Start();
                    return;
                }

                ActiveRecorder = new LibObsRecorder();
                Logger.WriteLine("Creating a new ActiveRecorder");
                await Task.Run(() => ActiveRecorder.Start());
            }
            else
            {
                Logger.WriteLine("Reusing old ActiveRecorder");
            }
        }

        public static void SetCurrentSession(int _Pid, string _GameTitle, string exeFile) {
            currentSession = new Session(_Pid, _GameTitle, exeFile);
        }

        public static Session GetCurrentSession() {
            return currentSession;
        }

        //[STAThread]
        public static async void StartRecording() {
            Logger.WriteLine("Is PreRecording " + IsRecording.ToString());
            Logger.WriteLine("Is Recording " + IsPreRecording.ToString());

            if (IsRecording || IsPreRecording) return;

            IsPreRecording = true;
            bool result = await ActiveRecorder.StartRecording();
            Logger.WriteLine("Start Success: " + result.ToString());
            Logger.WriteLine("Still allowed to record: " + (!IsRecording && result).ToString());
            if (!IsRecording && result) {
                Logger.WriteLine("Current Session PID: " + currentSession.Pid.ToString());
                if (currentSession.Pid != 0) {
                    recordingTimer.Elapsed += OnTimedEvent;
                    recordingTimer.Start();
                    
                    Logger.WriteLine(string.Format("Start Recording: {0}, {1}", currentSession.Pid, currentSession.GameTitle));
                    IsRecording = true;
                    IsPreRecording = false;

                    //frmMain.Instance.DisplayNotification("Recording Started", $"Currently recording {currentSession.GameTitle}");
                }
                //DetectionService.DisposeDetections();
            }
            if (!result) {
                // recorder failed to start properly so lets restart the currentSession Pid
                currentSession.Pid = 0;
            }
            IsPreRecording = false;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e) {
            recordingElapsed++;
            WebMessage.DisplayToast("Recording", currentSession.GameTitle, "🔴 Recording", "none", recordingElapsed);
        }

        public static async void StopRecording() {
            if (!IsRecording) return;

            bool result = await ActiveRecorder.StopRecording();

            if (IsRecording && result) {
                if(currentSession.Pid != 0) {
                    recordingElapsed = 0;
                    recordingTimer.Elapsed -= OnTimedEvent;
                    recordingTimer.Stop();
                    Logger.WriteLine(string.Format("Stop Recording: {0}, {1}", currentSession.Pid, currentSession.GameTitle));
                    currentSession.Pid = 0;
                    WebMessage.DestroyToast("Recording");
                    IsRecording = false;
                    StorageService.ManageStorage();
                }
                //DetectionService.LoadDetections();
            }
        }

        public static async void RestartRecording() {
            if (!IsRecording) return;

            bool stopResult = await ActiveRecorder.StopRecording();
            bool startResult = await ActiveRecorder.StartRecording();

            if (stopResult && startResult) {
                Logger.WriteLine("Recording restart successful");
            }
            else {
                Logger.WriteLine($"Issue trying to restart recording: {stopResult} {startResult}");
                IsRecording = false;
            }
        }
    }
}

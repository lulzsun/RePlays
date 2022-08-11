using RePlays.Recorders;
using RePlays.Utils;
using System.Timers;
using System;
using System.Threading.Tasks;

namespace RePlays.Services {
    public static class RecordingService {
        public static BaseRecorder ActiveRecorder = null;

        private static Timer recordingTimer = new Timer(1000);
        private static int recordingElapsed = 0;
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

        public static void Start(Type type) {
            DetectionService.LoadDetections();

            if (ActiveRecorder == null) {
                if (type == typeof(PlaysLTCRecorder)) {
                    ActiveRecorder = new PlaysLTCRecorder();
                    ActiveRecorder.Start();
                    return;
                }

                ActiveRecorder = new LibObsRecorder();
                ActiveRecorder.Start();
            }
        }

        public static void SetCurrentSession(int _Pid, string _GameTitle = "Game Unknown") {
            currentSession = new Session(_Pid, _GameTitle);
        }

        public static Session GetCurrentSession() {
            return currentSession;
        }

        public static async void StartRecording() {
            if (IsRecording || IsPreRecording) return;

            IsPreRecording = true;
            bool result = await ActiveRecorder.StartRecording();

            if (!IsRecording && result) {
                if(currentSession.Pid != 0) {
                    recordingTimer.Elapsed += OnTimedEvent;
                    recordingTimer.Start();
                    Logger.WriteLine(string.Format("Start Recording: {0}, {1}", currentSession.Pid, currentSession.GameTitle));
                    IsRecording = true;
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

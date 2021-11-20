using RePlays.Utils;
using System;
using System.Timers;

namespace RePlays.Services {
    public static class RecordingService {
        private static Timer recordingTimer = new Timer(1000);
        private static int recordingElapsed = 0;
        private static Session currentSession = new Session(0, "Game Unknown");
        public static bool IsRecording { get; internal set; }

        public class Session {
            public int Pid { get; internal set; }
            public string GameTitle { get; internal set; }
            public Session(int _Pid, string _GameTitle) {
                Pid = _Pid;
                GameTitle = _GameTitle;
            }
        }

        public static void SetCurrentSession(int _Pid, string _GameTitle = "Game Unknown") {
            currentSession = new Session(_Pid, _GameTitle);
        }

        public static Session GetCurrentSession() {
            return currentSession;
        }

        public static void StartRecording() {
            recordingTimer.Elapsed += OnTimedEvent;
            recordingTimer.Start();
            Logger.WriteLine(string.Format("Start Recording: {0}, {1}", currentSession.Pid, currentSession.GameTitle));
            IsRecording = true;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e) {
            recordingElapsed++;
            WebMessage.DisplayToast("Recording", currentSession.GameTitle, "🔴 Recording", "none", recordingElapsed);
        }

        public static void StopRecording() {
            recordingElapsed = 0;
            recordingTimer.Elapsed -= OnTimedEvent;
            recordingTimer.Stop();
            Logger.WriteLine(string.Format("Stop Recording: {0}, {1}", currentSession.Pid, currentSession.GameTitle));
            WebMessage.DestroyToast("Recording");
            IsRecording = false;
        }
    }
}

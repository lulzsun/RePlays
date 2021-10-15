using System;

namespace RePlays.Services {
    public static class RecordingService {
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
            Logger.WriteLine(string.Format("Start Recording: {0}, {1}", currentSession.Pid, currentSession.GameTitle));
            IsRecording = true;
        }

        public static void StopRecording() {
            Logger.WriteLine(string.Format("Stop Recording: {0}, {1}", currentSession.Pid, currentSession.GameTitle));
            IsRecording = false;
        }
    }
}

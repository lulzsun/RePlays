using System;

namespace RePlays.Services {
    public class RecordingService {
        private Session currentSession = new Session(0, "Game Unknown");
        public bool IsRecording { get; internal set; }

        public class Session {
            public int Pid { get; internal set; }
            public string GameTitle { get; internal set; }
            public Session(int _Pid, string _GameTitle) {
                Pid = _Pid;
                GameTitle = _GameTitle;
            }
        }

        public void SetCurrentSession(int _Pid, string _GameTitle = "Game Unknown") {
            currentSession = new Session(_Pid, _GameTitle);
        }

        public Session GetCurrentSession() {
            return currentSession;
        }

        public void StartRecording() {
            Logger.WriteLine(string.Format("Start Recording: {0}, {1}", currentSession.Pid, currentSession.GameTitle));
            IsRecording = true;
        }

        public void StopRecording() {
            Logger.WriteLine(string.Format("Stop Recording: {0}, {1}", currentSession.Pid, currentSession.GameTitle));
            IsRecording = false;
        }
    }
}

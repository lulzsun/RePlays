using System.Threading.Tasks;

namespace RePlays.Recorders {
    public abstract class BaseRecorder {
        public abstract void Start();
        public abstract void Stop();
        public abstract Task<bool> StartRecording();
        public abstract Task<bool> StopRecording();
    }
}

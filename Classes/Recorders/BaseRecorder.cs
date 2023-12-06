using System.Threading.Tasks;

namespace RePlays.Recorders {
    public abstract class BaseRecorder {
        public abstract void Start();
        public abstract Task<bool> StartRecording();
        public abstract Task<bool> StopRecording();
        public abstract void LostFocus();
        public abstract void GainedFocus();
    }
}
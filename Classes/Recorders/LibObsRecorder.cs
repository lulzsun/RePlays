using PlaysLTCWrapper;
using RePlays.Services;
using System.IO;
using System.Threading.Tasks;
using RePlays.Utils;
using static RePlays.Utils.Functions;

namespace RePlays.Recorders {
    public class LibObsRecorder : BaseRecorder {
        public bool Connected { get; private set; }

        public override void Start() {
            if (Connected) return;

            Connected = true;
            Logger.WriteLine("Successfully started LibObs!");
        }

        public override void Stop() {
            throw new System.NotImplementedException();
        }

        public override void StartRecording() {
            Logger.WriteLine("LibObs start recording");
        }

        public override void StopRecording() {
            Logger.WriteLine("LibObs stop recording");
        }
    }
}

using PlaysLTCWrapper;
using RePlays.Services;
using System.IO;
using System.Threading.Tasks;
using RePlays.Utils;
using static RePlays.Utils.Functions;

namespace RePlays.Recorders {
    public class PlaysLTCRecorder : BaseRecorder {
        private LTCProcess ltc = new LTCProcess();
        public bool Connected { get; private set; }

        public override void Start() {
            if (Connected) return;

            ltc.Log += (sender, msg) => {
                Logger.WriteLine(string.Format("{0}: {1}", msg.Title, msg.Message), msg.File, msg.Line);
            };

            ltc.ConnectionHandshake += (sender, msg) => {
                ltc.GetEncoderSupportLevel();
                ltc.SetSavePaths(GetPlaysFolder(), GetTempFolder());
                ltc.SetGameDVRQuality(
                    SettingsService.Settings.captureSettings.bitRate,
                    SettingsService.Settings.captureSettings.frameRate,
                    SettingsService.Settings.captureSettings.resolution
                );
                ltc.SetGameAudioVolume(
                    SettingsService.Settings.captureSettings.gameAudioVolume
                );
                ltc.SetMicAudioVolume(
                    SettingsService.Settings.captureSettings.micAudioVolume
                );
                ltc.SetMicRecordingDevice(
                    SettingsService.Settings.captureSettings.inputDevice.deviceId,
                    SettingsService.Settings.captureSettings.inputDevice.deviceLabel
                );
                ltc.SetCaptureMode(
                    SettingsService.Settings.captureSettings.recordingMode
                );
                ltc.SetGameDVRCaptureEngine(1); //1 = nvidia ?????
            };

            ltc.ProcessCreated += (sender, msg) => {
                if (!RecordingService.IsRecording) { // If we aren't already recording something, lets look for a process to record
                    bool isGame = DetectionService.IsMatchedGame(msg.ExeFile);
                    bool isNonGame = DetectionService.IsMatchedNonGame(msg.ExeFile);

                    if (isGame && !isNonGame) {
                        Logger.WriteLine(string.Format("This process [{0}] is a recordable game, preparing to LoadGameModule", msg.Pid));

                        string gameTitle = DetectionService.GetGameTitle(msg.ExeFile);
                        RecordingService.SetCurrentSession(msg.Pid, gameTitle);
                        ltc.SetGameName(gameTitle);
                        ltc.LoadGameModule(msg.Pid);
                    }
                    else if (!isGame && !isNonGame) {
                        Logger.WriteLine(string.Format("This process [{0}] is an unknown application, lets try to ScanForGraphLib", msg.Pid));

                        RecordingService.SetCurrentSession(0, DetectionService.GetGameTitle(msg.ExeFile));
                        ltc.ScanForGraphLib(msg.Pid); // the response will be sent to GraphicsLibLoaded if successful
                    }
                    else {
                        Logger.WriteLine(string.Format("This process [{0}] is a non-game", msg.Pid));
                    }
                }
                else {
                    Logger.WriteLine("Current recording a game right now, ignoring detection checks.");
                }
            };

            ltc.GraphicsLibLoaded += (sender, msg) => {
                RecordingService.GetCurrentSession().Pid = msg.Pid;
                ltc.SetGameName(RecordingService.GetCurrentSession().GameTitle);
                ltc.LoadGameModule(msg.Pid);
            };

            ltc.GameBehaviorDetected += (sender, msg) => {
                ltc.StartAutoHookedGame(msg.Pid);
            };

            ltc.VideoCaptureReady += (sender, msg) => {
                RecordingService.GetCurrentSession().Pid = msg.Pid;
                if (SettingsService.Settings.captureSettings.recordingMode == "automatic")
                    RecordingService.StartRecording();
            };

            ltc.ProcessTerminated += (sender, msg) => {                
                if (RecordingService.GetCurrentSession().Pid == msg.Pid)
                    RecordingService.StopRecording();
            };

            ltc.SaveFinished += async (sender, msg) => {
                try {
                    var t = await Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy));
                    WebMessage.SendMessage(t);
                }
                catch (System.Exception e) {
                    Logger.WriteLine(e.Message);
                }
            };

            Task.Run(() => ltc.Connect(Path.Join(GetPlaysLtcFolder(), "PlaysTVComm.exe")));
            Connected = true;
            Logger.WriteLine("Successfully started Plays-Ltc!");
        }

        public override void Stop() {
            throw new System.NotImplementedException();
        }

        public override Task<bool> StartRecording() {
            ltc.SetKeyBinds();
            ltc.StartRecording();
            return Task.FromResult(true);
        }

        public override Task<bool> StopRecording() {
            ltc.StopRecording();
            return Task.FromResult(true);
        }
    }
}

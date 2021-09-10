using PlaysLTCWrapper;
using RePlays.Services;
using System.IO;
using System.Threading.Tasks;
using static RePlays.Helpers.Functions;

namespace RePlays.Recorders {
    public static class PlaysLTC {
        private static LTCProcess ltc = new LTCProcess();
        public static bool Connected { get; private set; }

        public static void Start() {
            RecordingService recordingService = new RecordingService();
            DetectionService detectionService = new DetectionService();
            detectionService.DownloadGameDetections();
            detectionService.DownloadNonGameDetections();

            ltc.Log += (sender, msg) => {
                Logger.WriteLine(string.Format("{0}: {1}", msg.Title, msg.Message), msg.File, msg.Line);
            };

            ltc.ConnectionHandshake += (sender, msg) => {
                ltc.GetEncoderSupportLevel();
                ltc.SetSavePaths(GetPlaysFolder().Replace('\\', '/'), GetTempFolder().Replace('\\', '/'));
                ltc.SetGameDVRQuality(
                    SettingsService.Settings.gameDvrSettings.bitRate,
                    SettingsService.Settings.gameDvrSettings.frameRate,
                    SettingsService.Settings.gameDvrSettings.resolution
                );
                ltc.SetCaptureMode(49152); //ORB_GAMEDVR_SET_CAPTURE_MODE ?????
                ltc.SetGameDVRCaptureEngine(1); //1 = nvidia ?????
            };

            ltc.ProcessCreated += (sender, msg) => {
                if (!recordingService.IsRecording) { // If we aren't already recording something, lets look for a process to record
                    bool isGame = detectionService.IsMatchedGame(msg.ExeFile);
                    bool isNonGame = detectionService.IsMatchedNonGame(msg.ExeFile);

                    if (isGame && !isNonGame) {
                        Logger.WriteLine(string.Format("This process [{0}] is a recordable game, preparing to LoadGameModule", msg.Pid));

                        string gameTitle = detectionService.GetGameTitle(msg.ExeFile);
                        recordingService.SetCurrentSession(msg.Pid, gameTitle);
                        ltc.SetGameName(gameTitle);
                        ltc.LoadGameModule(msg.Pid);
                    }
                    else if (!isGame && !isNonGame) {
                        Logger.WriteLine(string.Format("This process [{0}] is an unknown application, lets try to ScanForGraphLib", msg.Pid));

                        recordingService.SetCurrentSession(msg.Pid, "Game Unknown");
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
                ltc.SetGameName("Game Unknown");
                ltc.LoadGameModule(msg.Pid);
            };

            ltc.GameBehaviorDetected += (sender, msg) => {
                ltc.StartAutoHookedGame(msg.Pid);
            };

            ltc.VideoCaptureReady += (sender, msg) => {
                //if (AutomaticRecording == true)
                if (!recordingService.IsRecording) {
                    ltc.SetKeyBinds();
                    ltc.StartRecording();
                    recordingService.StartRecording();
                }
            };

            ltc.ProcessTerminated += (sender, msg) => {
                if (recordingService.IsRecording) {
                    if (recordingService.GetCurrentSession().Pid == msg.Pid) {
                        ltc.StopRecording();
                        recordingService.StopRecording();
                    }
                }
            };

            Task.Run(() => ltc.Connect(Path.Join(GetPlaysLtcFolder(), "PlaysTVComm.exe")));
            Connected = true;
            Logger.WriteLine("Successfully started Plays-Ltc!");
        }
    }
}

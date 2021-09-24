using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RePlays.Controllers;
using RePlays.JSONObjects;
using RePlays.Recorders;
using RePlays.Services;
using static RePlays.Helpers.Functions;
using static RePlays.Services.SettingsService;

namespace RePlays.Messages {
    public class RetrieveVideos {
        public string game { get; set; }
        public string sortBy { get; set; }
    }

    public class ShowInFolder {
        private string _filePath;
        public string filePath {
            get {
                return _filePath.Replace("/", "\\");
            }
            set {
                _filePath = value;
            }
        }
    }

    public class Delete {
        private string[] _filePaths;
        public string[] filePaths {
            get {
                return _filePaths;
            }
            set {
                _filePaths = value;
                for (int i = 0; i < _filePaths.Length; i++) {
                    _filePaths[i] = _filePaths[i].Replace("/", "\\");
                }
            }
        }
    }

    public class CreateClips {
        private string _videoPath;
        public string videoPath {
            get {
                return _videoPath;
            }
            set {
                _videoPath = value.Replace("/", "\\");
            }
        }
        public ClipSegment[] clipSegments { get; set; }
    }

    public class WebMessage {
        public string message { get; set; }
        public string data { get; set; }

        public static VideoSortSettings videoSortSettings = new() {
            game = "All Games",
            sortBy = "Latest"
        };

        public static void SendMessage(string message) {
            frmMain.PostWebMessageAsJson(message);
        }

        public static async Task<WebMessage> RecieveMessage(string message) {
            WebMessage webMessage = JsonSerializer.Deserialize<WebMessage>(message);
            if (webMessage.data == null || webMessage.data.Trim() == string.Empty) webMessage.data = "{}";
            if (webMessage.message == "UpdateSettings")
                Logger.WriteLine($"{webMessage.message} ::: {"{Object too large to log}"}");
            else
                Logger.WriteLine($"{webMessage.message} ::: {webMessage.data}");

            switch (webMessage.message) {
                case "Initialize": {
                        // INIT USER SETTINGS
                        SendMessage(GetUserSettings());

                        // INIT RECORDER API
                        if (!File.Exists(Path.Join(GetPlaysLtcFolder(), "PlaysTVComm.exe"))) {
                            // path to old plays/replaystv's plays-ltc
                            var sourcePath = Path.Join(Environment.GetEnvironmentVariable("LocalAppData"), @"\Plays-ltc\0.54.7\");

                            if (!File.Exists(Path.Join(sourcePath, "PlaysTVComm.exe"))) {
                                SendMessage(DisplayModal("Did not detect a recording software. Would you like RePlays to automatically download and use PlaysLTC?", "Missing Recorder", "question"));
                                break;
                            }

                            Logger.WriteLine("Found Plays-ltc existing on local disk");
                            DirectoryCopy(sourcePath, GetPlaysLtcFolder(), true);
                            Logger.WriteLine("Copied Plays-ltc to recorders folder");
                            PlaysLTC.Start();
                        }
                    }
                    break;
                case "InstallPlaysLTC": {
                        bool downloadSuccess = await DownloadPlaysSetupAsync(frmMain.webView2.CoreWebView2);
                        bool installSuccess = await InstallPlaysSetup();
                        if (downloadSuccess && installSuccess) {
                            SendMessage(DisplayModal("PlaysLTC successfully installed!", "Install Success", "success"));
                            PlaysLTC.Start();
                        }
                        else {
                            SendMessage(DisplayModal("Failed to install PlaysLTC", "Install Failed", "warning"));
                        }
                    }
                    break;
                case "UpdateSettings": {
                        SettingsJson data = JsonSerializer.Deserialize<SettingsJson>(webMessage.data);
                        SaveSettings(data);
                    }
                    break;
                case "RetrieveVideos": {
                        RetrieveVideos data = JsonSerializer.Deserialize<RetrieveVideos>(webMessage.data);
                        videoSortSettings.game = data.game;
                        videoSortSettings.sortBy = data.sortBy;
                        var t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy));
                        SendMessage(t);
                    }
                    break;
                case "ShowInFolder": {
                        ShowInFolder data = JsonSerializer.Deserialize<ShowInFolder>(webMessage.data);
                        var filePath = Path.Join(GetPlaysFolder(), data.filePath);
                        Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
                    }
                    break;
                case "Delete": {
                        Delete data = JsonSerializer.Deserialize<Delete>(webMessage.data);
                        foreach (var filePath in data.filePaths) {
                            var realFilePath = Path.Join(GetPlaysFolder(), filePath);
                            var thumbPath = Path.Join(Path.GetDirectoryName(realFilePath), @"\.thumbs\", Path.GetFileNameWithoutExtension(realFilePath) + ".png");

                            VideoController.DisposeOpenStreams();

                            File.Delete(realFilePath);
                            File.Delete(thumbPath);
                        }
                        var t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy));
                        SendMessage(t);
                    }
                    break;
                case "CreateClips": {
                        CreateClips data = JsonSerializer.Deserialize<CreateClips>(webMessage.data);
                        var t = await Task.Run(() => CreateClip(data.videoPath, data.clipSegments));
                        if (t == null) {
                            SendMessage(DisplayModal("Failed to create clip", "Error", "warning"));
                        }
                        else {
                            SendMessage(DisplayModal("Successfully created clip", "Success", "success"));
                            t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy));
                            SendMessage(t);
                        }
                    }
                    break;
                default:
                    break;
            }

            return webMessage;
        }
    }
}
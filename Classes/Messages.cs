using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RePlays.JSONObjects;
using RePlays.Recorders;
using RePlays.Services;
using static RePlays.Helpers.Functions;

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

        public static async Task<WebMessage> RecieveMessage(Microsoft.Web.WebView2.WinForms.WebView2 webView2, string message) {
            WebMessage webMessage = JsonSerializer.Deserialize<WebMessage>(message);
            if (webMessage.data == null || webMessage.data.Trim() == string.Empty) webMessage.data = "{}";
            Logger.WriteLine($"{webMessage.message} ::: {webMessage.data}");

            switch (webMessage.message) {
                case "Initialize": {
                        if (!File.Exists(Path.Join(GetPlaysLtcFolder(), "PlaysTVComm.exe"))) {
                            // path to old plays/replaystv's plays-ltc
                            var sourcePath = Path.Join(Environment.GetEnvironmentVariable("LocalAppData"), @"\Plays-ltc\0.54.7\");

                            if (!File.Exists(Path.Join(sourcePath, "PlaysTVComm.exe"))) {
                                webView2.CoreWebView2.PostWebMessageAsJson(DisplayModal("Did not detect a recording software. Would you like RePlays to automatically download and use PlaysLTC?", "Missing Recorder", "question"));
                                break;
                            }

                            Logger.WriteLine("Found Plays-ltc existing on local disk");
                            DirectoryCopy(sourcePath, GetPlaysLtcFolder(), true);
                            Logger.WriteLine("Copied Plays-ltc to recorders folder");
                        }
                        PlaysLTC.Start();
                    }
                    break;
                case "InstallPlaysLTC": {
                        bool downloadSuccess = await DownloadPlaysSetupAsync(webView2.CoreWebView2);
                        bool installSuccess = await InstallPlaysSetup();
                        if (downloadSuccess && installSuccess) {
                            webView2.CoreWebView2.PostWebMessageAsJson(DisplayModal("PlaysLTC successfully installed!", "Install Success", "success"));
                            PlaysLTC.Start();
                        }
                        else {
                            webView2.CoreWebView2.PostWebMessageAsJson(DisplayModal("Failed to install PlaysLTC", "Install Failed", "warning"));
                        }
                    }
                    break;
                case "RetrieveVideos": {
                        RetrieveVideos data = JsonSerializer.Deserialize<RetrieveVideos>(webMessage.data);
                        videoSortSettings.game = data.game;
                        videoSortSettings.sortBy = data.sortBy;
                        var t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy));
                        webView2.CoreWebView2.PostWebMessageAsJson(t);
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
                            File.Delete(Path.Join(GetPlaysFolder(), filePath));
                        }
                        var t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy));
                        webView2.CoreWebView2.PostWebMessageAsJson(t);
                    }
                    break;
                case "CreateClips": {
                        CreateClips data = JsonSerializer.Deserialize<CreateClips>(webMessage.data);
                        var t = await Task.Run(() => CreateClip(data.videoPath, data.clipSegments));
                        if (t == null) {
                            webView2.CoreWebView2.PostWebMessageAsJson(DisplayModal("Failed to create clip", "Error", "warning"));
                        }
                        else {
                            webView2.CoreWebView2.PostWebMessageAsJson(DisplayModal("Successfully created clip", "Success", "success"));
                            t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy));
                            webView2.CoreWebView2.PostWebMessageAsJson(t);
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
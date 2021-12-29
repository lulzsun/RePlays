using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using RePlays.Recorders;
using RePlays.Services;
using static RePlays.Utils.Functions;
using static RePlays.Services.SettingsService;

namespace RePlays.Utils {
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

    public class UploadVideo {
        private string _destination;
        public string destination {
            get {
                return _destination;
            }
            set {
                _destination = value;
            }
        }
        private string _title;
        public string title {
            get {
                return _title;
            }
            set {
                _title = value;
            }
        }
        private string _file;
        public string file {
            get {
                return _file;
            }
            set {
                _file = value;
            }
        }
    }

    public class RemoveProgram {
        public string list { get; set; }
        public string exe { get; set; }
    }

    public class WebMessage {
        public string message { get; set; }
        public string data { get; set; }

        public static Dictionary<string, WebMessage> toastList = new();
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
                case "GetAudioDevices": {
                        frmMain.webView2.CoreWebView2.Navigate(GetRePlaysURI());
                    }
                    break;
                case "Initialize": {
                        // INIT USER SETTINGS
                        SendMessage(GetUserSettings());

                        // INIT RECORDER API
                        if (!File.Exists(Path.Join(GetPlaysLtcFolder(), "PlaysTVComm.exe"))) {
                            // path to old plays/replaystv's plays-ltc
                            var sourcePath = Path.Join(Environment.GetEnvironmentVariable("LocalAppData"), @"\Plays-ltc\0.54.7\");

                            if (!File.Exists(Path.Join(sourcePath, "PlaysTVComm.exe"))) {
                                DisplayModal("Did not detect a recording software. Would you like RePlays to automatically download and use PlaysLTC?", "Missing Recorder", "question");
                                break;
                            }

                            Logger.WriteLine("Found Plays-ltc existing on local disk");
                            DirectoryCopy(sourcePath, GetPlaysLtcFolder(), true);
                            Logger.WriteLine("Copied Plays-ltc to recorders folder");
                            RecordingService.Start(typeof(PlaysLTC));
                        }
                        else {
                            RecordingService.Start(typeof(PlaysLTC));
                        }

                        Logger.WriteLine(toastList.Count + " Initialized List");
                        foreach (var toast in toastList) {
                            SendMessage(JsonSerializer.Serialize(toast.Value));
                        }
                    }
                    break;
                case "InstallPlaysLTC": {
                        bool downloadSuccess = await DownloadPlaysSetupAsync();
                        bool installSuccess = await InstallPlaysSetup();
                        if (downloadSuccess && installSuccess) {
                            DisplayModal("PlaysLTC successfully installed!", "Install Success", "success");
                            RecordingService.Start(typeof(PlaysLTC));
                        }
                        else {
                            DisplayModal("Failed to install PlaysLTC", "Install Failed", "warning");
                        }
                    }
                    break;
                case "UpdateSettings": {
                        SaveSettings(webMessage);
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
                case "EditKeybind": {
                        var id = webMessage.data.Replace("\"", "");
                        frmMain.Instance.EditKeybind(id);
                    }
                    break;
                case "SelectFolder": {
                        var type = webMessage.data.Replace("\"", "");
                        switch (type) {
                            case "videoSaveDir":
                            case "tempSaveDir":
                                using (var fbd = new FolderBrowserDialog()) {
                                    DialogResult result = fbd.ShowDialog();

                                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                                        if (type == "videoSaveDir") Settings.advancedSettings.videoSaveDir = fbd.SelectedPath;
                                        else if (type == "tempSaveDir") Settings.advancedSettings.videoSaveDir = fbd.SelectedPath;
                                        SaveSettings();
                                        SendMessage(GetUserSettings());
                                    }
                                }
                                break;
                            case "extraSaveDir":
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case "ShowFolder": {
                        var path = webMessage.data.Replace("\"", "").Replace("\\\\", "\\");
                        Logger.WriteLine(path);
                        Process.Start("explorer.exe", path);
                    }
                    break;
                case "ShowInFolder": {
                        ShowInFolder data = JsonSerializer.Deserialize<ShowInFolder>(webMessage.data);
                        var filePath = Path.Join(GetPlaysFolder(), data.filePath).Replace('/', '\\');
                        Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
                    }
                    break;
                case "ShowLicense": {
                        Process.Start("notepad.exe", Path.Join(Application.StartupPath, @"LICENSE"));
                    }
                    break;
                case "ShowLogs": {
                        Process.Start("explorer.exe", string.Format("/select,\"{0}\"", Path.Join(Application.StartupPath, @"cfg\logs.txt")));
                    }
                    break;
                case "Delete": {
                        Delete data = JsonSerializer.Deserialize<Delete>(webMessage.data);
                        foreach (var filePath in data.filePaths) {
                            var realFilePath = Path.Join(GetPlaysFolder(), filePath);
                            var thumbPath = Path.Join(Path.GetDirectoryName(realFilePath), @"\.thumbs\", Path.GetFileNameWithoutExtension(realFilePath) + ".png");

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
                            DisplayModal("Failed to create clip", "Error", "warning");
                        }
                        else {
                            DisplayModal("Successfully created clip", "Success", "success");
                            t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy));
                            SendMessage(t);
                        }
                    }
                    break;
                case "UploadVideo": {
                        UploadVideo data = JsonSerializer.Deserialize<UploadVideo>(webMessage.data);
                        var filePath = Path.Join(GetPlaysFolder(), data.file);
                        if(File.Exists(filePath)) {
                            Logger.WriteLine($"Preparing to upload {filePath} to {data.destination}");
                            UploadService.Upload(data.destination, data.title, filePath);
                        }
                        //DisplayModal($"{data.title} {data.destination}", "Error", "warning"));
                    }
                    break;
                case "ShowRecentLinks": {
                        frmMain.Instance.PopulateRecentLinks();
                    }
                    break;
                case "HideRecentLinks": {
                        frmMain.Instance.HideRecentLinks();
                    }
                    break;
                case "AddProgram": {
                        var list = webMessage.data.Replace("\"", "");
                        switch (list) {
                            case "blacklist":
                            case "whitelist":
                                using (var fbd = new OpenFileDialog()) {
                                    fbd.Filter = "Executable files (*.exe)|*.exe";
                                    DialogResult result = fbd.ShowDialog();

                                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.FileName)) {
                                        if (list == "blacklist") {
                                            Settings.advancedSettings.blacklist.Add(fbd.FileName.ToLower());
                                            Logger.WriteLine($"Added {fbd.FileName} to blacklist");
                                        }
                                        else if (list == "whitelist") {
                                            Settings.advancedSettings.whitelist.Add(fbd.FileName.ToLower());
                                            Logger.WriteLine($"Added {fbd.FileName} to whitelist");
                                        }
                                        SaveSettings();
                                        SendMessage(GetUserSettings());
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case "RemoveProgram": {
                        RemoveProgram data = JsonSerializer.Deserialize<RemoveProgram>(webMessage.data);
                        Logger.WriteLine($"{data.exe} | {data.list}");
                        switch (data.list) {
                            case "blacklist":
                                Settings.advancedSettings.blacklist.Remove(data.exe.ToLower());
                                break;
                            case "whitelist":
                                Settings.advancedSettings.whitelist.Remove(data.exe.ToLower());
                                break;
                            default:
                                break;
                        }
                        SaveSettings();
                        SendMessage(GetUserSettings());
                    }
                    break;
                default:
                    break;
            }

            return webMessage;
        }

        public static void DisplayModal(string context, string title = "Title", string icon = "none", long progress = 0, long progressMax = 0) {
            WebMessage webMessage = new();
            webMessage.message = "DisplayModal";
            webMessage.data = "{" +
                "\"context\": \"" + context + "\", " +
                "\"title\": \"" + title + "\", " +
                "\"progress\": " + progress + ", " +
                "\"progressMax\": " + progressMax + ", " +
                "\"icon\": \"" + icon + "\"}";
            if (frmMain.Instance.WindowState != FormWindowState.Minimized)
                SendMessage(JsonSerializer.Serialize(webMessage));
        }

        public static void DisplayToast(string id, string context, string title = "Title", string icon = "none", long progress = 0, long progressMax = 0) {
            WebMessage webMessage = new();
            webMessage.message = "DisplayToast";
            webMessage.data = "{" +
                "\"id\": \"" + id + "\", " +
                "\"context\": \"" + context + "\", " +
                "\"title\": \"" + title + "\", " +
                "\"progress\": " + progress + ", " +
                "\"progressMax\": " + progressMax + ", " +
                "\"icon\": \"" + icon + "\"}";

            if (toastList.ContainsKey(id)) {
                if (toastList[id].data == webMessage.data) return; // prevents message flooding if toast is identical
                toastList[id] = webMessage;
            }
            else toastList.Add(id, webMessage);

            if (frmMain.Instance.WindowState != FormWindowState.Minimized)
                SendMessage(JsonSerializer.Serialize(webMessage));
        }

        public static void DestroyToast(string id) {
            if (toastList.ContainsKey(id))
                toastList.Remove(id);

            WebMessage webMessage = new();
            webMessage.message = "DestroyToast";
            webMessage.data = "{" +
                "\"id\": \"" + id + "\"}";
            SendMessage(JsonSerializer.Serialize(webMessage));
        }
    }
}
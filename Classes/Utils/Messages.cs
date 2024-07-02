using RePlays.Classes.Utils;
using RePlays.Recorders;
using RePlays.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static RePlays.Services.SettingsService;
using static RePlays.Utils.Compression;
using static RePlays.Utils.Functions;

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

    public class CompressClip {
        private string _filePath;
        public string filePath {
            get {
                return _filePath.Replace("/", "\\");
            }
            set {
                _filePath = value;
            }
        }

        public string game { get; set; }
        public string quality { get; set; }
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
        public string game { get; set; }
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
        private string _game;
        public string game {
            get {
                return _game;
            }
            set {
                _game = value;
            }
        }
        private bool _makePublic;
        public bool makePublic {
            get {
                return _makePublic;
            }
            set {
                _makePublic = value;
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
        public string userAgent { get; set; }

        public static Dictionary<string, WebMessage> modalList = new();
        public static Dictionary<string, WebMessage> toastList = new();
        public static VideoSortSettings videoSortSettings = new() {
            game = "All Games",
            sortBy = "Latest"
        };

        public static bool SendMessage(string message) {
            List<WebSocket> activeSockets = WebServer.GetActiveSockets();
            foreach (var socket in activeSockets) {
                var responseMessage = Encoding.UTF8.GetBytes(message);
                Task.Run(() => {
                    socket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None);
                }).Wait();
            }
#if WINDOWS
            if (WindowsInterface.webView2 == null || WindowsInterface.webView2.IsDisposed == true || WindowsInterface.webView2.Source.AbsolutePath.Contains("preload")) return false;
            if (WindowsInterface.webView2.InvokeRequired) {
                // Call this same method but make sure it is on UI thread
                return WindowsInterface.webView2.Invoke(new Func<bool>(() => {
                    return SendMessage(message);
                }));
            }
            else {
                WindowsInterface.webView2.CoreWebView2.PostWebMessageAsJson(message);
                return true;
            }
#endif
            return true;
        }

        public static async Task<WebMessage> ReceiveMessage(string message) {
            if (message == null) return null;
            WebMessage webMessage = JsonSerializer.Deserialize<WebMessage>(message);
            if (webMessage.data == null || webMessage.data.Trim() == string.Empty) webMessage.data = "{}";
            bool isReplaysWebView = false;
            if (webMessage.userAgent != null && webMessage.userAgent.Equals("RePlays/WebView")) isReplaysWebView = true;
            if (webMessage.message == "UpdateSettings")
                Logger.WriteLine($"{webMessage.message} ::: {"{Object too large to log}"}");
            else
                Logger.WriteLine($"{webMessage.message} ::: {webMessage.data}{webMessage.userAgent ?? $" : {webMessage.userAgent}"}");

            switch (webMessage.message) {
                case "BrowserReady": {
                        GetAudioDevices();
#if WINDOWS
#if DEBUG
                        // TODO: Make WebServer optional through a setting
                        // Serve video files/thumbnails to allow the frontend to use them
                        WebServer.Start();
#endif
                        WindowsInterface.webView2.CoreWebView2.Navigate(GetRePlaysURI());
#endif
                        break;
                    }
                case "Initialize": {
                        // INIT USER SETTINGS
                        SendMessage(GetUserSettings());

                        Logger.WriteLine($"Initializing {toastList.Count} Toasts");
                        foreach (var toast in toastList) {
                            SendMessage(JsonSerializer.Serialize(toast.Value));
                        }

                        Logger.WriteLine($"Initializing {modalList.Count} Modals");
                        foreach (var modal in modalList) {
                            SendMessage(JsonSerializer.Serialize(modal.Value));
                            modalList.Remove(modal.Key);
                        }
                    }
                    break;
                case "CheckForUpdates": {
                        Updater.CheckForUpdates(webMessage.data.Contains("true"));
                    }
                    break;
                case "UpdateSettings": {
                        SaveSettings(webMessage);
                        SendMessage(GetUserSettings());
                    }
                    break;
                case "RetrieveVideos": {
                        RetrieveVideos data = JsonSerializer.Deserialize<RetrieveVideos>(webMessage.data);
                        videoSortSettings.game = data.game;
                        videoSortSettings.sortBy = data.sortBy;
                        var t = await Task.Run(() => GetAllVideos(
                            videoSortSettings.game, videoSortSettings.sortBy, isReplaysWebView
                        ));
                        SendMessage(t);
                    }
                    break;
                case "EnterEditKeybind": {
                        var id = webMessage.data.Replace("\"", "");
                        KeybindService.EditId = id;
                    }
                    break;
                case "ExitEditKeybind": {
                        var id = webMessage.data.Replace("\"", "");
                        KeybindService.EditId = null;
                    }
                    break;
#if WINDOWS
                case "SelectFolder": {
                        var type = webMessage.data.Replace("\"", "");
                        switch (type) {
                            case "extraSaveDir":
                                break;
                            default:
                                using (var fbd = new System.Windows.Forms.FolderBrowserDialog()) {
                                    System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                                    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
                                        if (type == "videoSaveDir") Settings.storageSettings.videoSaveDir = fbd.SelectedPath;
                                        else if (type == "tempSaveDir") Settings.storageSettings.tempSaveDir = fbd.SelectedPath;
                                        else if (type == "localFolderDir") Settings.uploadSettings.localFolderSettings.dir = fbd.SelectedPath;
                                        SaveSettings();
                                        SendMessage(GetUserSettings());
                                        var t = await Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy, true));
                                        WebMessage.SendMessage(t);
                                    }
                                }
                                break;
                        }
                    }
                    break;
#endif
                case "ShowFolder": {
                        var path = webMessage.data.Replace("\"", "").Replace("\\\\", "\\");
                        Logger.WriteLine(path);
#if WINDOWS
                        Process.Start("explorer.exe", path.Replace('/', '\\'));
#else
                        Process.Start("xdg-open", path);
#endif
                    }
                    break;
                case "OpenLink": {
                        Process browserProcess = new();
                        browserProcess.StartInfo.UseShellExecute = true;
                        browserProcess.StartInfo.FileName = webMessage.data;
                        browserProcess.Start();
                    }
                    break;
                case "CompressClip": {
                        CompressClip data = JsonSerializer.Deserialize<CompressClip>(webMessage.data);
                        string filePath = Path.Join(GetPlaysFolder(), data.filePath).Replace('\\', '/');
                        CompressFile(filePath, data);
                    }
                    break;
                case "ShowInFolder": {
                        ShowInFolder data = JsonSerializer.Deserialize<ShowInFolder>(webMessage.data);
                        var filePath = Path.Join(GetPlaysFolder(), data.filePath).Replace('\\', '/');
#if WINDOWS
                        Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath.Replace('/', '\\')));
#else
                        Process.Start("dolphin", $"--select \"{filePath}\"");
#endif
                    }
                    break;
                case "ShowLicense": {
#if WINDOWS
                        Process.Start("notepad.exe", Path.Join(GetStartupPath(), @"LICENSE"));
#else
                        Process.Start("xdg-open", Path.Join(GetStartupPath(), @"LICENSE"));
#endif
                    }
                    break;
                case "ShowLogs": {
                        var logsPath = Path.GetFullPath(Path.Join(GetStartupPath(), "../cfg/logs.txt"));
                        if (!File.Exists(logsPath))
                            logsPath = Path.GetFullPath(Path.Join(GetStartupPath(), "../../cfg/logs.txt"));
                        if (!File.Exists(logsPath))
                            break;
                        if (File.Exists(logsPath))
#if WINDOWS
                            Process.Start("explorer.exe", string.Format("/select,\"{0}\"", logsPath.Replace('/', '\\')));
#else
                            Process.Start("xdg-open", logsPath);
#endif
                    }
                    break;
                case "Delete": {
                        Delete data = JsonSerializer.Deserialize<Delete>(webMessage.data);
                        foreach (var filePath in data.filePaths) {
                            var realFilePath = Path.Join(GetPlaysFolder(), filePath.Replace("\\", "/"));
                            var successfulDelete = false;
                            var failedLoops = 0;
                            while (!successfulDelete) {
                                try {
                                    DeleteVideo(realFilePath);
                                    successfulDelete = true;
                                }
                                catch (Exception e) {
                                    if (failedLoops == 5) {
                                        DisplayModal("Failed to delete video (in use by another process?) \n " + realFilePath, "Delete Failed", "warning");
                                        Logger.WriteLine($"Failed to delete video: {e.Message}");
                                        break;
                                    }
                                    await Task.Delay(2000);
                                    failedLoops++;
                                }
                            }
                        }
                        var t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy, isReplaysWebView));
                        SendMessage(t);
                    }
                    break;
                case "CreateClips": {
                        CreateClips data = JsonSerializer.Deserialize<CreateClips>(webMessage.data);
                        var t = await Task.Run(() => CreateClip(data.game, data.videoPath, data.clipSegments));
                        if (t == null) {
                            DisplayModal("Failed to create clip", "Error", "warning");
                        }
                        else {
                            DisplayModal("Successfully created clip", "Success", "success");
                            t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy, isReplaysWebView));
                            SendMessage(t);
                        }
                    }
                    break;
                case "UploadVideo": {
                        UploadVideo data = JsonSerializer.Deserialize<UploadVideo>(webMessage.data);
                        var filePath = Path.Join(GetPlaysFolder(), data.file);
                        if (File.Exists(filePath)) {
                            Logger.WriteLine($"Preparing to upload {filePath} to {data.destination}");
                            UploadService.Upload(data.destination, data.title, filePath, data.game, data.makePublic);
                        }
                        //DisplayModal($"{data.title} {data.destination}", "Error", "warning"));
                    }
                    break;
                case "DownloadNvidiaAudioSDK": {
                        DownloadNvidiaAudioSDK();
                    }
                    break;
#if WINDOWS
                case "ShowRecentLinks": {
                        WindowsInterface.Instance.PopulateRecentLinks();
                    }
                    break;
                case "HideRecentLinks": {
                        WindowsInterface.Instance.HideRecentLinks();
                    }
                    break;
                case "AddProgram": {
                        var list = webMessage.data.Replace("\"", "");
                        using var fbd = new System.Windows.Forms.OpenFileDialog();
                        fbd.Filter = "Executable files (*.exe)|*.exe";
                        System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                        if (result != System.Windows.Forms.DialogResult.OK && string.IsNullOrWhiteSpace(fbd.FileName)) break;
                        switch (list) {
                            case "blacklist":
                                Settings.detectionSettings.blacklist.Add(fbd.FileName.ToLower());
                                Logger.WriteLine($"Added {fbd.FileName} to blacklist");
                                break;
                            case "whitelist":
                                Settings.detectionSettings.whitelist.Add(new CustomGame(fbd.FileName.ToLower(), Path.GetFileName(fbd.FileName)));
                                Logger.WriteLine($"Added {fbd.FileName} to custom games");
                                break;
                        }
                        SaveSettings();
                        SendMessage(GetUserSettings());
                    }
                    break;
#endif
                case "RemoveProgram": {
                        RemoveProgram data = JsonSerializer.Deserialize<RemoveProgram>(webMessage.data);
                        Logger.WriteLine($"{data.exe} | {data.list}");
                        switch (data.list) {
                            case "blacklist":
                                Settings.detectionSettings.blacklist.Remove(data.exe.ToLower());
                                break;
                            case "whitelist":
                                Settings.detectionSettings.whitelist.RemoveAll((x) => x.gameExe == data.exe);
                                break;
                            default:
                                break;
                        }
                        SaveSettings();
                        SendMessage(GetUserSettings());
                    }
                    break;
                case "StopRecording": {
                        RecordingService.StopRecording(true);
                    }
                    break;
#if WINDOWS
                case "Restart": {
                        string path = Path.Join(GetStartupPath(), @"../RePlays.exe");
                        string cmdCommand = $"/C timeout /t 1 & start \"\" \"{path}\"";

                        ProcessStartInfo processInfo = new ProcessStartInfo {
                            FileName = "cmd.exe",
                            Arguments = cmdCommand,
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };

                        Process.Start(processInfo);
                        Process.GetCurrentProcess().Kill(); // this is not a clean exit, need to look into why we can't cleanly exit
                    }
                    break;
#endif
                default:
                    break;
            }

            return webMessage;
        }

        public static async void DisplayModal(string context, string title = "Title", string icon = "none", long progress = 0, long progressMax = 0) {
            WebMessage webMessage = new();
            webMessage.message = "DisplayModal";
            webMessage.data = "{" +
                "\"context\": \"" + context + "\", " +
                "\"title\": \"" + title + "\", " +
                "\"progress\": " + progress + ", " +
                "\"progressMax\": " + progressMax + ", " +
                "\"icon\": \"" + icon + "\"}";

            bool success = SendMessage(JsonSerializer.Serialize(webMessage));
            if (!success) {
                // if message was not successful (interface was probably minimized), save to list to show later
                modalList.Add(context, webMessage);
            }
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

        public static void SetBookmarks(string videoName, List<Bookmark> bookmarks, double elapsed) {
            string json = "{" +
                    "\"videoname\": \"" + videoName + "\", " +
                    "\"elapsed\": " + elapsed.ToString().Replace(",", ".") + ", " +
                    "\"bookmarks\": " + JsonSerializer.Serialize(bookmarks) + "}";
#if WINDOWS
            if (WindowsInterface.webView2 != null) {
                WebMessage webMessage = new();
                webMessage.message = "SetBookmarks";
                webMessage.data = json;
                SendMessage(JsonSerializer.Serialize(webMessage));
                Logger.WriteLine("Successfully sent bookmarks to frontend");
            }
            else {
                BackupBookmarks(videoName, json);
            }
#endif
        }
    }
}
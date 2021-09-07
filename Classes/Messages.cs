using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Replays.JSONObjects;
using static Replays.Helpers.Functions;

namespace Replays.Messages
{
    public class RetrieveVideos
    {
        public string game { get; set; }
        public string sortBy { get; set; }
    }

    public class ShowInFolder
    {
        private string _filePath;
        public string filePath
        {
            get
            {
                return _filePath.Replace("/", "\\");
            }
            set
            {
                _filePath = value;
            }
        }
    }

    public class Delete
    {
        private string[] _filePaths;
        public string[] filePaths
        {
            get
            {
                return _filePaths;
            }
            set
            {
                _filePaths = value;
                for (int i = 0; i < _filePaths.Length; i++)
                {
                    _filePaths[i] = _filePaths[i].Replace("/", "\\");
                }
            }
        }
    }

    public class CreateClips
    {
        private string _videoPath;
        public string videoPath
        {
            get
            {
                return _videoPath;
            }
            set
            {
                _videoPath = value.Replace("/", "\\");
            }
        }
        public ClipSegment[] clipSegments { get; set; }
    }

    public class WebMessage
    {
        public string message { get; set; }
        public string data { get; set; }

        public static VideoSortSettings videoSortSettings = new()
        {
            game = "All Games",
            sortBy = "Latest"
        };

        public static async void RecieveMessage(Microsoft.Web.WebView2.WinForms.WebView2 webView2, string message)
        {
            WebMessage webMessage = JsonSerializer.Deserialize<WebMessage>(message);
            Console.WriteLine($"{webMessage.message} ::: {webMessage.data}");

            switch (webMessage.message)
            {
                case "Initialize":
                    {
                        if (!File.Exists(Path.Join(GetPlaysLtcFolder(), "PlaysTVComm.exe")))
                        {
                            // path to old plays/replaystv's plays-ltc
                            //var sourcePath = Path.Join(Environment.GetEnvironmentVariable("LocalAppData"), @"\Plays-ltc\0.54.7\");
                            //if (File.Exists(Path.Join(sourcePath, "PlaysTVComm.exe")))
                            //{
                            //    Console.WriteLine("Found Plays-ltc existing on local disk");
                            //    DirectoryCopy(sourcePath, GetPlaysLtcFolder(), true);
                            //    Console.WriteLine("Copied Plays-ltc to recorders folder");
                            //    break;
                            //}

                            webView2.CoreWebView2.PostWebMessageAsJson(DisplayModal("Did not detect a recording software. Would you like to RePlays to automatically download and use PlaysLTC?", "Missing Recorder", "question"));
                            break;
                        }
                        Console.WriteLine("Ready to record with PlaysLTC");
                    }
                    break;
                case "InstallPlaysLTC":
                    {
                        var downloadSuccess = await DownloadPlaysSetupAsync();
                        if (downloadSuccess)
                        {
                            webView2.CoreWebView2.PostWebMessageAsJson(DisplayModal("PlaysLTC successfully installed!", "Install Success", "success"));
                        }
                        else
                        {
                            webView2.CoreWebView2.PostWebMessageAsJson(DisplayModal("Failed to install PlaysLTC", "Install Failed", "warning"));
                        }
                    }
                    break;
                case "RetrieveVideos":
                    {
                        RetrieveVideos data = JsonSerializer.Deserialize<RetrieveVideos>(webMessage.data);
                        videoSortSettings.game = data.game;
                        videoSortSettings.sortBy = data.sortBy;
                        var t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy));
                        webView2.CoreWebView2.PostWebMessageAsJson(t);
                    }
                    break;
                case "ShowInFolder":
                    {
                        ShowInFolder data = JsonSerializer.Deserialize<ShowInFolder>(webMessage.data);
                        var filePath = Path.Join(GetPlaysFolder(), data.filePath);
                        Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
                    }
                    break;
                case "Delete":
                    {
                        Delete data = JsonSerializer.Deserialize<Delete>(webMessage.data);
                        foreach (var filePath in data.filePaths)
                        {
                            File.Delete(Path.Join(GetPlaysFolder(), filePath));
                        }
                        var t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy));
                        webView2.CoreWebView2.PostWebMessageAsJson(t);
                    }
                    break;
                case "CreateClips":
                    {
                        CreateClips data = JsonSerializer.Deserialize<CreateClips>(webMessage.data);
                        var t = await Task.Run(() => CreateClip(data.videoPath, data.clipSegments));
                        if(t == null)
                        {
                            webView2.CoreWebView2.PostWebMessageAsJson(DisplayModal("Failed to create clip", "Error", "warning"));
                        }
                        else
                        {
                            webView2.CoreWebView2.PostWebMessageAsJson(DisplayModal("Successfully created clip", "Success", "success"));
                            t = await Task.Run(() => GetAllVideos(videoSortSettings.game, videoSortSettings.sortBy));
                            webView2.CoreWebView2.PostWebMessageAsJson(t);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
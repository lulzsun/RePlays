using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WinFormsApp
{
    public partial class frmMain : Form
    {
        public const string PlaysFolder = @"G:\Videos\Plays";
        public class WebMessage
        {
            public string message { get; set; }
            public string data { get; set; }
        }
        public class VideoList
        {
            public List<Video> sessions { get; set; }
            public long sessionsSize { get; set; }
            public List<Video> clips { get; set; }
            public long clipsSize { get; set; }
        }
        public class Video
        {
            public DateTime date { get; set; }
            public string type { get; set; }
            public long size { get; set; }
            public string game { get; set; }
            public string path { get; set; }
            public string thumbnail { get; set; }
        }
        public frmMain()
        {
            InitializeComponent();
        }
        private static string GetAllVideos()
        {
            var allfiles = Directory.GetFiles(PlaysFolder, "*.mp4*", SearchOption.AllDirectories).OrderByDescending(d => new FileInfo(d).CreationTime);

            VideoList videoList = new();
            videoList.sessions = new();
            videoList.clips = new();

            foreach (string file in allfiles)
            {
                Video video = new();
                video.size = new FileInfo(file).Length;
                video.date = new FileInfo(file).CreationTime;
                video.path = (@"Plays\" + file.Split(@"\Plays\")[1]).Replace('\\', '/');
                video.game = Path.GetFileName(Path.GetDirectoryName(file));
                if (file.EndsWith("-ses.mp4"))
                {
                    videoList.sessions.Add(video);
                    videoList.sessionsSize += video.size;
                }
                else
                {
                    videoList.clips.Add(video);
                    videoList.clipsSize += video.size;
                }
                video.thumbnail = (@"Plays\" + GetOrCreateThumbnail(file).Split(@"\Plays\")[1]).Replace('\\', '/');
            }

            WebMessage webMessage = new();
            webMessage.message = "RetrieveVideos";
            webMessage.data = JsonSerializer.Serialize(videoList);
            return JsonSerializer.Serialize(webMessage);
        }

        public static string GetOrCreateThumbnail(string videoPath)
        {
            string thumbnailPath = Path.Combine(Path.GetDirectoryName(videoPath), ".thumbs\\", Path.GetFileNameWithoutExtension(videoPath) + ".png");

            if (File.Exists(thumbnailPath)) return thumbnailPath;

            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "ffmpeg.exe",
                Arguments = "-ss 00:00:01.000 -y -i " + '"' + videoPath + '"' + " -vframes 1 -s 1024x576 " + '"' + thumbnailPath + '"',
            };

            var process = new Process
            {
                StartInfo = startInfo
            };
            process.Start();
            process.WaitForExit();
            process.Close();

            Debug.WriteLine("Created new thumbnail: {0}", thumbnailPath);
            return thumbnailPath;
        }

        private async void CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            await webView21.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.setIgnoreCertificateErrors", "{\"ignore\": true}");
            webView21.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView21.CoreWebView2.Settings.IsWebMessageEnabled = true;
        }

        private async void WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            WebMessage webMessage = JsonSerializer.Deserialize<WebMessage>(e.WebMessageAsJson);
            Debug.WriteLine($"{webMessage.message} ::: {webMessage.data}");

            switch (webMessage.message)
            {
                case "RetrieveVideos":
                    var t = await Task.Run(() => GetAllVideos());
                    webView21.CoreWebView2.PostWebMessageAsJson(t);
                    break;
                default:
                    break;
            }
        }
    }
}

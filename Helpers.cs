using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.WindowsAPICodePack.Shell;
using Replays.JSONObjects;
using Replays.Messages;

namespace Replays.Helpers
{
    public static class Functions
    {
        public static string GetPlaysFolder()
        {
            return @"G:\Videos\Plays";
        }

        public static string GetFFmpegFolder()
        {
#if DEBUG
            return Path.Join(Directory.GetCurrentDirectory(), @"ClientApp\node_modules\ffmpeg-ffprobe-static\");
#else
            return Path.Join(Application.StartupPath, @"ClientApp\node_modules\ffmpeg-ffprobe-static\");
#endif
        }

        public static string GetAllVideos(string Game = "All Games", string SortBy = "Latest")
        {
            var allfiles = (dynamic)null;
            switch (SortBy)
            {
                case "Latest":
                    allfiles = Directory.GetFiles(GetPlaysFolder(), "*.mp4*", SearchOption.AllDirectories).OrderByDescending(d => new FileInfo(d).CreationTime);
                    break;
                case "Oldest":
                    allfiles = Directory.GetFiles(GetPlaysFolder(), "*.mp4*", SearchOption.AllDirectories).OrderBy(d => new FileInfo(d).CreationTime);
                    break;
                case "Smallest":
                    allfiles = Directory.GetFiles(GetPlaysFolder(), "*.mp4*", SearchOption.AllDirectories).OrderBy(d => new FileInfo(d).Length);
                    break;
                case "Largest":
                    allfiles = Directory.GetFiles(GetPlaysFolder(), "*.mp4*", SearchOption.AllDirectories).OrderByDescending(d => new FileInfo(d).Length);
                    break;
                default:
                    return "{}";
            }

            VideoList videoList = new();
            videoList.game = Game;
            videoList.games = new();
            videoList.sortBy = SortBy;
            videoList.sessions = new();
            videoList.clips = new();

            foreach (string file in allfiles)
            {
                Video video = new();
                video.size = new FileInfo(file).Length;
                video.date = new FileInfo(file).CreationTime;
                video.fileName = Path.GetFileName(file);
                video.game = Path.GetFileName(Path.GetDirectoryName(file));

                if (!videoList.games.Contains(video.game)) videoList.games.Add(video.game);

                if (!Game.Equals(Path.GetFileName(Path.GetDirectoryName(file))) && !Game.Equals("All Games")) continue;

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
                video.thumbnail = Path.GetFileName(GetOrCreateThumbnail(file));
            }

            videoList.games.Sort();

            WebMessage webMessage = new();
            webMessage.message = "RetrieveVideos";
            webMessage.data = JsonSerializer.Serialize(videoList);
            return JsonSerializer.Serialize(webMessage);
        }

        public static string GetOrCreateThumbnail(string videoPath)
        {
            string thumbnailPath = Path.Combine(Path.GetDirectoryName(videoPath), ".thumbs\\", Path.GetFileNameWithoutExtension(videoPath) + ".png");

            if (File.Exists(thumbnailPath)) return thumbnailPath;

            //var startInfo = new ProcessStartInfo
            //{
            //    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
            //    CreateNoWindow = true,
            //    UseShellExecute = false,
            //    FileName = "ffmpeg.exe",
            //    Arguments = "-ss 00:00:01.000 -y -i " + '"' + videoPath + '"' + " -vframes 1 -s 1024x576 " + '"' + thumbnailPath + '"',
            //};

            //var process = new Process
            //{
            //    StartInfo = startInfo
            //};
            //process.Start();
            //process.WaitForExit();
            //process.Close();

            ShellFile shellFile = ShellFile.FromFilePath(videoPath);
            Bitmap shellThumb = shellFile.Thumbnail.ExtraLargeBitmap;
            shellThumb.Save(thumbnailPath, ImageFormat.Png);
            Debug.WriteLine("Created new thumbnail: {0}", thumbnailPath);

            return thumbnailPath;
        }
    }
}
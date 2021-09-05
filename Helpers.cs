using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms; // exists for Application.StartupPath
using Replays.JSONObjects;
using Replays.Messages;

namespace Replays.Helpers
{
    public static class Functions
    {
        public static string MessageError(string message)
        {
            WebMessage webMessage = new();
            webMessage.message = "MessageError";
            webMessage.data = "{\"message\": \"" + message + "\"}";
            return JsonSerializer.Serialize(webMessage);
        }
        public static string MessageSuccess(string message)
        {
            WebMessage webMessage = new();
            webMessage.message = "MessageSuccess";
            webMessage.data = "{\"message\": \"" + message + "\"}";
            return JsonSerializer.Serialize(webMessage);
        }
        public static string GetPlaysFolder()
        {
            return @"G:\Videos\Plays";
        }

        public static string GetTempFolder()
        {
            return @"G:\Videos\Plays\.temp";
        }

        public static string GetFFmpegFolder()
        {

#if DEBUG
            string ffmpegFolder = Path.Join(Directory.GetCurrentDirectory(), @"ClientApp\node_modules\ffmpeg-ffprobe-static\");
#else
            string ffmpegFolder = Path.Join(Application.StartupPath, @"ClientApp\node_modules\ffmpeg-ffprobe-static\");
#endif
            if(Directory.Exists(ffmpegFolder))
            {
                return ffmpegFolder;
            }
            else
            {
                throw new DirectoryNotFoundException(ffmpegFolder);
            }
        }

                public static string Get7zipFolder()
        {

#if DEBUG
            string _7zipFolder = Path.Join(Directory.GetCurrentDirectory(), @"ClientApp\node_modules\7zip-bin\win\x64\");
#else
            string _7zipFolder = Path.Join(Application.StartupPath, @"ClientApp\node_modules\7zip-bin\win\x64\");
#endif
            if(Directory.Exists(_7zipFolder))
            {
                return _7zipFolder;
            }
            else
            {
                throw new DirectoryNotFoundException(_7zipFolder);
            }
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
                if (!(file.EndsWith("-ses.mp4") || file.EndsWith("-clp.mp4"))) continue;

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

        public static double GetVideoDuration(string videoPath)
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffprobe.exe"),
                Arguments = string.Format("-i \"{0}\" -show_entries format=duration -v quiet -of csv=\"p = 0\"", videoPath),
            };

            var process = new Process
            {
                StartInfo = startInfo
            };
            process.Start();
            double duration = 0;
            try
            {
                duration = Convert.ToDouble(process.StandardOutput.ReadToEnd().Replace("\r\n", ""));
            }
            catch (Exception)
            {
                // if exception happens, usually means video is not valid
                Console.WriteLine(process.StandardOutput.ReadToEnd().Replace("\r\n", ""));
            }
            process.WaitForExit();
            process.Close();

            return duration;
        }

        public static string GetOrCreateThumbnail(string videoPath)
        {
            string thumbnailPath = Path.Combine(Path.GetDirectoryName(videoPath), ".thumbs\\", Path.GetFileNameWithoutExtension(videoPath) + ".png");

            if (File.Exists(thumbnailPath)) return thumbnailPath;

            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffmpeg.exe"),
                Arguments = string.Format("-ss {0} -y -i \"{1}\" -vframes 1 -s 1024x576 \"{2}\"", GetVideoDuration(videoPath) / 2, videoPath, thumbnailPath),
            };
            
            var process = new Process
            {
                StartInfo = startInfo
            };
            process.Start();
            process.WaitForExit();
            process.Close();

            Console.WriteLine(string.Format("Created new thumbnail: {0}", thumbnailPath));

            return thumbnailPath;
        }

        public static string CreateClip(string videoPath, ClipSegment[] clipSegments, int index=0)
        {
            string inputFile = Path.Join(GetPlaysFolder(), videoPath);
            string outputFile = Path.Combine(Path.GetDirectoryName(inputFile), DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "-clp.mp4");

            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffmpeg.exe"),
            };

            if (clipSegments.Length > 1 && index != clipSegments.Length)
            {
                if (index == 0) File.Delete(Path.Join(GetTempFolder(), "list.txt"));
                outputFile = Path.Join(GetTempFolder(), "temp" + index + ".mp4");
                File.AppendAllLines(Path.Join(GetTempFolder(), "list.txt"), new[] { "file '" + outputFile + "'" });
            }
            if (clipSegments.Length > 1 && index == clipSegments.Length)
            {
                startInfo.Arguments =
                    "-f concat -safe 0 -i \"" + Path.Join(GetTempFolder(), "list.txt") + "\" -codec copy \"" + outputFile + "\"";
                Console.WriteLine(startInfo.Arguments);
            }
            else
            {
                startInfo.Arguments =
                    "-ss " + clipSegments[index].start + " " +
                    "-i \"" + inputFile + "\" " +
                    "-t " + clipSegments[index].duration + " -codec copy " +
                    "-y \"" + outputFile + "\"";
            }

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                Console.WriteLine("O: " + e.Data);
            });
            process.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                Console.WriteLine("E: " + e.Data);
            });

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();

            if (!File.Exists(outputFile)) return null;

            if (clipSegments.Length > 1 && index != clipSegments.Length) return CreateClip(videoPath, clipSegments, index+1);
            else if (clipSegments.Length > 1 && index == clipSegments.Length) Console.WriteLine(string.Format("Created new multiclip: {0}", outputFile));
            else Console.WriteLine(string.Format("Created new clip: {0}", outputFile));

            return outputFile;
        }

        public static void PurgeTempVideos()
        {
            var tempVideos = Directory.GetFiles(GetTempFolder(), "*.mp4*", SearchOption.AllDirectories);

            if (tempVideos.Length == 0) return;

            Console.WriteLine("Purging temporary video files");

            foreach (string video in tempVideos)
            {
                try
                {
                    File.Delete(video);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to delete video {0} : {1}", video, e);
                }
            }
        }
    }
}
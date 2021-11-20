using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms; // exists for Application.StartupPath
using RePlays.Services;

namespace RePlays.Utils {
    public static class Functions {
        public static string GenerateShortID() {
            var ticks = new DateTime(2021, 1, 1).Ticks;
            var ans = DateTime.Now.Ticks - ticks;
            return ans.ToString("x");
        }

        public static string GetPlaysLtcFolder() {
            //var path = Path.Join(Application.StartupPath, @"Plays-ltc\0.54.7\"); this doesnt work for some reason, plays-ltc has to be in the localappdata folder
            var path = Environment.GetEnvironmentVariable("LocalAppData") + @"\Plays-ltc\0.54.7\";
            return path;
        }

        public static string GetPlaysFolder() {
            var videoSaveDir = SettingsService.Settings.advancedSettings.videoSaveDir;
            if (!Directory.Exists(videoSaveDir))
                Directory.CreateDirectory(videoSaveDir);
            return videoSaveDir;
        }

        public static string GetTempFolder() {
            var tempSaveDir = SettingsService.Settings.advancedSettings.tempSaveDir;
            if (!Directory.Exists(tempSaveDir))
                Directory.CreateDirectory(tempSaveDir);
            return tempSaveDir;
        }

        public static string GetCfgFolder() {
            var cfgDir = Path.Join(Application.StartupPath, @"cfg\");
            if (!Directory.Exists(cfgDir))
                Directory.CreateDirectory(cfgDir);
            return cfgDir;
        }

        public static string GetFFmpegFolder() {

#if DEBUG
            string ffmpegFolder = Path.Join(Directory.GetCurrentDirectory(), @"ClientApp\node_modules\ffmpeg-ffprobe-static\");
#else
            string ffmpegFolder = Path.Join(Application.StartupPath, @"ClientApp\node_modules\ffmpeg-ffprobe-static\");
#endif
            if (Directory.Exists(ffmpegFolder)) {
                return ffmpegFolder;
            }
            else {
                throw new DirectoryNotFoundException(ffmpegFolder);
            }
        }

        public static string Get7zipExecutable() {
#if DEBUG
            string _7zipExecutable = Path.Join(Directory.GetCurrentDirectory(), @"ClientApp\node_modules\7zip-bin\win\x64\7za.exe");
#else
            string _7zipExecutable = Path.Join(Application.StartupPath, @"ClientApp\node_modules\7zip-bin\win\x64\7za.exe");
#endif
            if (File.Exists(_7zipExecutable)) {
                return _7zipExecutable;
            }
            else {
                throw new FileNotFoundException(_7zipExecutable);
            }
        }

        public static async Task<bool> DownloadPlaysSetupAsync() {
            var playsSetupDir = Path.Join(GetTempFolder() + @"\PlaysSetup.exe");
            var correctHash = "e12c1740e7ff672fcbb33c2d35cfb4f557b53f37b94653cf8170af2e074e1622";

            if (File.Exists(playsSetupDir))
                if (SHA256Compare(playsSetupDir, correctHash)) return true;

            Logger.WriteLine("PlaysSetup.exe missing or failed checksum, starting download");
            WebMessage.DisplayModal("Downloading PlaysSetup.exe from web.archive.org", "Downloading", "none", 0, 145310344);
            using (var client = new WebClient()) {
                client.DownloadProgressChanged += (o, args) => {
                    WebMessage.DisplayModal("Downloading PlaysSetup.exe from web.archive.org", "Downloading", "none", args.BytesReceived, 145310344);
                };
                client.DownloadFileCompleted += (o, args) => {
                    Logger.WriteLine("Finished downloading PlaysSetup.exe");
                    WebMessage.DisplayModal("Finished downloading PlaysSetup.exe, doing a checksum", "Downloading", "info");
                };
                await client.DownloadFileTaskAsync(
                    new Uri("https://web.archive.org/web/20191212211927if_/https://app-updates.plays.tv/builds/PlaysSetup.exe"),
                    playsSetupDir);
            }
            return File.Exists(playsSetupDir) && SHA256Compare(playsSetupDir, correctHash);
        }

        public static async Task<bool> InstallPlaysSetup() {
            bool install = false; bool result = true;
            string playsSetup = Path.Join(GetTempFolder() + @"\PlaysSetup.exe");

            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                FileName = Path.Join(Get7zipExecutable()),
            };

            startInfo.Arguments = "e \"" + Path.Join(GetTempFolder(), @"\Plays-3.0.0-full.nupkg") + "\" -o\"" + GetPlaysLtcFolder() + "\" lib\\net45\\resources\\ltc\\*.*";

            if (File.Exists(playsSetup) && !File.Exists(Path.Join(GetTempFolder(), @"\Plays-3.0.0-full.nupkg"))) {
                startInfo.Arguments = "e \"" + playsSetup + "\" -o\"" + GetTempFolder() + "\" Plays-3.0.0-full.nupkg";
                install = true;
            }

            Logger.WriteLine(startInfo.Arguments);

            var process = new Process {
                StartInfo = startInfo
            };

            process.OutputDataReceived += new DataReceivedEventHandler((s, e) => {
                Logger.WriteLine("O: " + e.Data);
            });
            process.ErrorDataReceived += new DataReceivedEventHandler((s, e) => {
                if (e.Data != null && e.Data.Trim() != String.Empty) {
                    Logger.WriteLine("E: " + e.Data);
                    result = false;
                }
            });

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();

            if (install && File.Exists(Path.Join(GetTempFolder(), @"\Plays-3.0.0-full.nupkg"))) InstallPlaysSetup();
            return result;
        }

        public static bool SHA256Compare(string filePath, string compare) {
            using (SHA256 SHA256 = SHA256Managed.Create()) {
                using (FileStream fileStream = File.OpenRead(filePath)) {
                    byte[] bytes = SHA256.ComputeHash(fileStream);

                    // Convert byte array to a string   
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++) {
                        builder.Append(bytes[i].ToString("x2"));
                    }
                    return builder.ToString().Equals(compare);
                }
            }
        }

        public static string GetUserSettings() {
            SettingsService.LoadSettings();

            WebMessage webMessage = new();
            webMessage.message = "UserSettings";
            webMessage.data = JsonSerializer.Serialize(SettingsService.Settings);
            return JsonSerializer.Serialize(webMessage);
        }

        public static string GetAllVideos(string Game = "All Games", string SortBy = "Latest") {
            VideoList videoList = GetAllVideos(Game, SortBy, true);
            if (videoList == null) return "{}";
            WebMessage webMessage = new();
            webMessage.message = "RetrieveVideos";
            webMessage.data = JsonSerializer.Serialize(videoList);
            return JsonSerializer.Serialize(webMessage);
        }

        public static VideoList GetAllVideos(string Game = "All Games", string SortBy = "Latest", bool isVideoList = true) {
            var allfiles = (dynamic)null;
            switch (SortBy) {
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
                    return null;
            }

            VideoList videoList = new();
            videoList.game = Game;
            videoList.games = new();
            videoList.sortBy = SortBy;
            videoList.sessions = new();
            videoList.clips = new();

            foreach (string file in allfiles) {
                if (!(file.EndsWith("-ses.mp4") || file.EndsWith("-clp.mp4")) || !File.Exists(file)) continue;

                Video video = new();
                video.size = new FileInfo(file).Length;
                video.date = new FileInfo(file).CreationTime;
                video.fileName = Path.GetFileName(file);
                video.game = Path.GetFileName(Path.GetDirectoryName(file));

                if (!videoList.games.Contains(video.game)) videoList.games.Add(video.game);

                if (!Game.Equals(Path.GetFileName(Path.GetDirectoryName(file))) && !Game.Equals("All Games")) continue;

                var thumb = GetOrCreateThumbnail(file);
                if (!File.Exists(thumb)) continue;
                video.thumbnail = Path.GetFileName(thumb);

                if (file.EndsWith("-ses.mp4")) {
                    videoList.sessions.Add(video);
                    videoList.sessionsSize += video.size;
                }
                else {
                    videoList.clips.Add(video);
                    videoList.clipsSize += video.size;
                }
            }

            videoList.games.Sort();

            return videoList;
        }

        public static double GetVideoDuration(string videoPath) {
            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffprobe.exe"),
                Arguments = string.Format("-i \"{0}\" -show_entries format=duration -v quiet -of csv=\"p = 0\"", videoPath),
            };

            var process = new Process {
                StartInfo = startInfo
            };
            process.Start();
            double duration = 0;
            try {
                duration = Convert.ToDouble(process.StandardOutput.ReadToEnd().Replace("\r\n", ""));
            }
            catch (Exception) {
                // if exception happens, usually means video is not valid
                Logger.WriteLine(process.StandardOutput.ReadToEnd().Replace("\r\n", ""));
            }
            process.WaitForExit();
            process.Close();

            return duration;
        }

        public static string GetOrCreateThumbnail(string videoPath) {
            string thumbsDir = Path.Combine(Path.GetDirectoryName(videoPath), ".thumbs\\");
            string thumbnailPath = Path.Combine(thumbsDir, Path.GetFileNameWithoutExtension(videoPath) + ".png");

            if (File.Exists(thumbnailPath)) return thumbnailPath;
            if (!Directory.Exists(thumbsDir)) Directory.CreateDirectory(thumbsDir);

            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffmpeg.exe"),
                Arguments = string.Format("-ss {0} -y -i \"{1}\" -vframes 1 -s 1024x576 \"{2}\"", GetVideoDuration(videoPath) / 2, videoPath, thumbnailPath),
            };

            var process = new Process {
                StartInfo = startInfo
            };
            process.Start();
            process.WaitForExit();
            process.Close();

            Logger.WriteLine(string.Format("Created new thumbnail: {0}", thumbnailPath));

            return thumbnailPath;
        }

        public static string CreateClip(string videoPath, ClipSegment[] clipSegments, int index = 0) {
            string inputFile = Path.Join(GetPlaysFolder(), videoPath);
            string outputFile = Path.Combine(Path.GetDirectoryName(inputFile), DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "-clp.mp4");

            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffmpeg.exe"),
            };

            if (clipSegments.Length > 1 && index != clipSegments.Length) {
                if (index == 0) File.Delete(Path.Join(GetTempFolder(), "list.txt"));
                outputFile = Path.Join(GetTempFolder(), "temp" + index + ".mp4");
                File.AppendAllLines(Path.Join(GetTempFolder(), "list.txt"), new[] { "file '" + outputFile + "'" });
            }
            if (clipSegments.Length > 1 && index == clipSegments.Length) {
                startInfo.Arguments =
                    "-f concat -safe 0 -i \"" + Path.Join(GetTempFolder(), "list.txt") + "\" -codec copy \"" + outputFile + "\"";
                Logger.WriteLine(startInfo.Arguments);
            }
            else {
                startInfo.Arguments =
                    "-ss " + clipSegments[index].start + " " +
                    "-i \"" + inputFile + "\" " +
                    "-t " + clipSegments[index].duration + " -codec copy " +
                    "-y \"" + outputFile + "\"";
            }

            var process = new Process {
                StartInfo = startInfo
            };

            process.OutputDataReceived += new DataReceivedEventHandler((s, e) => {
                Logger.WriteLine("O: " + e.Data);
            });
            process.ErrorDataReceived += new DataReceivedEventHandler((s, e) => {
                Logger.WriteLine("E: " + e.Data);
            });

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();

            if (!File.Exists(outputFile)) return null;

            if (clipSegments.Length > 1 && index != clipSegments.Length) return CreateClip(videoPath, clipSegments, index + 1);
            else if (clipSegments.Length > 1 && index == clipSegments.Length) Logger.WriteLine(string.Format("Created new multiclip: {0}", outputFile));
            else Logger.WriteLine(string.Format("Created new clip: {0}", outputFile));

            return outputFile;
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs) {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files) {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs) {
                foreach (DirectoryInfo subdir in dirs) {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        public static void PurgeTempVideos() {
            var tempVideos = Directory.GetFiles(GetTempFolder(), "*.mp4*", SearchOption.AllDirectories);

            if (tempVideos.Length == 0) return;

            Logger.WriteLine("Purging temporary video files");

            foreach (string video in tempVideos) {
                try {
                    File.Delete(video);
                }
                catch (Exception e) {
                    Logger.WriteLine(string.Format("Failed to delete video {0} : {1}", video, e));
                }
            }
        }

        public static string EncryptString(string stringToEncrypt, string optionalEntropy = null) {
            return Convert.ToBase64String(
                ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(stringToEncrypt)
                    , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                    , DataProtectionScope.CurrentUser));
        }

        public static string DecryptString(string encryptedString, string optionalEntropy = null) {
            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedString)
                    , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                    , DataProtectionScope.CurrentUser));
        }
    }
}
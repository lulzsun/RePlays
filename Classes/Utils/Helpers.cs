using RePlays.Recorders;
using RePlays.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Process = System.Diagnostics.Process;
using Timer = System.Timers.Timer;

namespace RePlays.Utils {
    public static class Functions {
        static string[] programArgs;

        public static void SetProgramArgs(string[] args) {
            programArgs = args;
        }

        public static string[] GetProgramArgs() {
            return programArgs;
        }

#if DEBUG
        public static string GetSolutionPath() {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory != null && !directory.GetFiles("*.sln").Any()) {
                directory = directory.Parent;
            }
            if (directory != null) {
                return directory.FullName;
            }
            try {
                return Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;
            }
            catch (NullReferenceException) {
                return Directory.GetCurrentDirectory();
            }
        }
#endif
        public static void PlaySound(string fileName) {
#if WINDOWS
            System.Media.SoundPlayer bookmarkSound = new(fileName);
            bookmarkSound.Play();
#endif
        }

        public static string GenerateShortID() {
            var ticks = new DateTime(2021, 1, 1).Ticks;
            var ans = DateTime.Now.Ticks - ticks;
            return ans.ToString("x");
        }

        public static string GetStartupPath() {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static string GetRePlaysURI() {
#if DEBUG 
            if (GetProgramArgs().Any("--use-build-ui".Contains)) {
                return "file://" + GetSolutionPath() + "/ClientApp/build/index.html";
            }
            return "http://localhost:3000/#/";
#else
            return "file://" + GetStartupPath() + "/ClientApp/build/index.html";
#endif
        }

        public static string GetPlaysLtcFolder() {
            //var path = Path.Join(GetStartupPath(), @"Plays-ltc\0.54.7\"); this doesnt work for some reason, plays-ltc has to be in the localappdata folder
            var path = Environment.GetEnvironmentVariable("LocalAppData") + @"\Plays-ltc\0.54.7\";
            return path;
        }

        public static string GetPlaysFolder() {
            var videoSaveDir = SettingsService.Settings.storageSettings.videoSaveDir.Replace('\\', '/');
            if (!DriveInfo.GetDrives().Where(drive => drive.Name.StartsWith(videoSaveDir[..1])).Any()) {
                SettingsService.Settings.storageSettings.videoSaveDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Plays");
                SettingsService.SaveSettings();
#if WINDOWS
                if (WindowsInterface.webView2 == null) {
                    Task.Run(() => SendDisplayModalWithDelay("The program was unable to access the drive. As a result, the storage location has been reverted to the default location.", "Drive Disconnected", "info", 10000));
                }
                else {
                    WebMessage.DisplayModal("The program was unable to access the drive. As a result, the storage location has been reverted to the default location.", "Drive Disconnected", "info");
                }
#endif
                return SettingsService.Settings.storageSettings.videoSaveDir.Replace('\\', '/');
            }

            if (!Directory.Exists(videoSaveDir))
                Directory.CreateDirectory(videoSaveDir);
            return videoSaveDir;
        }

        public static async Task SendDisplayModalWithDelay(string context, string title, string icon, int delay) {
            await Task.Delay(delay);
            WebMessage.DisplayModal(context, title, icon);
        }

        public static string GetTempFolder() {
            var tempSaveDir = SettingsService.Settings.storageSettings.tempSaveDir.Replace('\\', '/');
            if (!Directory.Exists(tempSaveDir))
                Directory.CreateDirectory(tempSaveDir);
            return tempSaveDir;
        }

        public static string GetCfgFolder() {
            var cfgDir = Path.Join(GetStartupPath(), @"../cfg/");
            if (!Directory.Exists(cfgDir))
                Directory.CreateDirectory(cfgDir);
            return cfgDir;
        }

        public static string GetResourcesFolder() {
#if (DEBUG)
            return GetSolutionPath() + @"/Resources/";
#elif (RELEASE)
            return GetStartupPath() + @"/Resources/";
#endif
        }

        public static string GetFFmpegFolder() {
#if !WINDOWS
            string ffmpegFolder = Path.Join(GetSolutionPath(), @"ClientApp/node_modules/ffmpeg-ffprobe-static/");
            if (File.Exists(Path.Join(ffmpegFolder, "ffmpeg")) && File.Exists(Path.Join(ffmpegFolder, "ffprobe"))) {
#else
            string ffmpegFolder = GetStartupPath();
            if (File.Exists(Path.Join(ffmpegFolder, "ffmpeg.exe")) && File.Exists(Path.Join(ffmpegFolder, "ffprobe.exe"))) {
#endif
                return ffmpegFolder;
            }
            else {
                throw new FileNotFoundException($"Missing ffmpeg and/or ffprobe in '{ffmpegFolder}'");
            }
        }

        public static string Get7zipExecutable() {
#if DEBUG
            string _7zipExecutable = Path.Join(GetSolutionPath(), @"ClientApp\node_modules\7zip-bin\win\x64\7za.exe");
#else
            string _7zipExecutable = Path.Join(GetStartupPath(), @"ClientApp\node_modules\7zip-bin\win\x64\7za.exe");
#endif
            if (File.Exists(_7zipExecutable)) {
                return _7zipExecutable;
            }
            else {
                throw new FileNotFoundException(_7zipExecutable);
            }
        }

        public static string MakeValidFolderNameSimple(string folderName) {
            if (string.IsNullOrEmpty(folderName)) return folderName;

            foreach (var c in Path.GetInvalidFileNameChars())
                folderName = folderName.Replace(c.ToString(), string.Empty);

            foreach (var c in Path.GetInvalidPathChars())
                folderName = folderName.Replace(c.ToString(), string.Empty);

            return folderName;
        }

        public static void GetAudioDevices() {
            var outputCache = SettingsService.Settings.captureSettings.outputDevicesCache;
            var inputCache = SettingsService.Settings.captureSettings.inputDevicesCache;
            outputCache.Clear();
            inputCache.Clear();
            outputCache.Add(new("default", "Default Device"));
            inputCache.Add(new("default", "Default Device", false));
#if WINDOWS
            outputCache.Add(new("communications", "Default Communication Device"));
            inputCache.Add(new("communications", "Default Communication Device", false));
            ManagementObjectSearcher searcher = new("Select * From Win32_PnPEntity");
            ManagementObjectCollection deviceCollection = searcher.Get();
            foreach (ManagementObject obj in deviceCollection) {
                if (obj == null) continue;
                if (obj.Properties["PNPClass"].Value == null) continue;

                if (obj.Properties["PNPClass"].Value.ToString() == "AudioEndpoint") {
                    string id = obj.Properties["PNPDeviceID"].Value.ToString().Split('\\').Last();
                    AudioDevice dev = new(id, obj.Properties["Name"].Value.ToString()) {
                        deviceId = id.ToLower(),
                        deviceLabel = obj.Properties["Name"].Value.ToString(),
                        deviceVolume = 100
                    };
                    if (id.StartsWith("{0.0.0.00000000}")) outputCache.Add(dev);
                    else inputCache.Add(dev);
                    Logger.WriteLine(dev.deviceId + " | " + dev.deviceLabel);

                }
            }
#else
            // TODO: Get audio devices on Linux
#endif
            if (SettingsService.Settings.captureSettings.inputDevices.Count == 0) {
                SettingsService.Settings.captureSettings.inputDevices.Add(inputCache[0]);
            }
            if (SettingsService.Settings.captureSettings.outputDevices.Count == 0) {
                SettingsService.Settings.captureSettings.outputDevices.Add(outputCache[0]);
            }
            SettingsService.SaveSettings();
        }

        public static string GetUserSettings() {
            SettingsService.LoadSettings();

            WebMessage webMessage = new() {
                message = "UserSettings",
                data = JsonSerializer.Serialize(SettingsService.Settings)
            };
            return JsonSerializer.Serialize(webMessage);
        }

        public static string GetAllVideos(string game, string sortBy, bool isRePlaysWebView = false) {
            VideoList videoList = GetAllVideos(game, sortBy, true, isRePlaysWebView);
            if (videoList == null) return "{}";
            WebMessage webMessage = new() {
                message = "RetrieveVideos",
                data = JsonSerializer.Serialize(videoList)
            };
            return JsonSerializer.Serialize(webMessage);
        }

        public static VideoList GetAllVideos(string game, string sortBy, bool isVideoList, bool isRePlaysWebView = false) {
            var videoExtensions = new[] { ".mp4", ".mkv", ".mov", ".flv" };
            List<FileInfo> allfiles = [];
            switch (sortBy) {
                case "Latest":
                    allfiles = new DirectoryInfo(GetPlaysFolder())
                        .EnumerateFiles("*.*", SearchOption.AllDirectories)
                        .Where(file => videoExtensions.Any(file.Extension.Equals))
                        .OrderByDescending(file => file.CreationTime)
                        .ToList();
                    break;
                case "Oldest":
                    allfiles = new DirectoryInfo(GetPlaysFolder())
                        .EnumerateFiles("*.*", SearchOption.AllDirectories)
                        .Where(file => videoExtensions.Any(file.Extension.Equals))
                        .OrderBy(file => file.CreationTime)
                        .ToList();
                    break;
                case "Smallest":
                    allfiles = new DirectoryInfo(GetPlaysFolder())
                        .EnumerateFiles("*.*", SearchOption.AllDirectories)
                        .Where(file => videoExtensions.Any(file.Extension.Equals))
                        .OrderBy(file => file.Length)
                        .ToList();
                    break;
                case "Largest":
                    allfiles = new DirectoryInfo(GetPlaysFolder())
                        .EnumerateFiles("*.*", SearchOption.AllDirectories)
                        .Where(file => videoExtensions.Any(file.Extension.Equals))
                        .OrderByDescending(file => file.Length)
                        .ToList();
                    break;
                default:
                    return null;
            }

            VideoList videoList = new() {
                game = game,
                games = [],
                sortBy = sortBy,
                sessions = [],
                clips = []
            };

            Logger.WriteLine($"Found '{allfiles.Count}' video files in {GetPlaysFolder()}");

            foreach (FileInfo file in allfiles) {
                var fileWithoutExt = Path.GetFileNameWithoutExtension(file.FullName);
                if (!(fileWithoutExt.EndsWith("-ses") || fileWithoutExt.EndsWith("-man") || fileWithoutExt.EndsWith("-clp")) || !file.Exists) continue;

                Video video = new() {
                    size = file.Length,
                    metadata = GetOrCreateMetadata(file.FullName),
                    date = file.CreationTime,
                    fileName = Path.GetFileName(file.FullName),
                    game = Path.GetFileName(Path.GetDirectoryName(file.FullName)),
                };

#if DEBUG && WINDOWS
                video.folder = "http://localhost:3001/"; // if not using web server: https://videos.replays.app/
#else
                if (isRePlaysWebView)
                    video.folder = "file://" + Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file.FullName), "..")).Replace("\\", "/");
                else
                    video.folder = "http://localhost:3001/";
#endif

                if (!videoList.games.Contains(video.game)) videoList.games.Add(video.game);

                if (!game.Equals(Path.GetFileName(Path.GetDirectoryName(file.FullName))) && !game.Equals("All Games")) continue;

                var thumb = GetOrCreateThumbnail(file.FullName, video.metadata.duration);
                if (!File.Exists(thumb)) continue;
                video.thumbnail = Path.GetFileName(thumb);

                if (fileWithoutExt.EndsWith("-ses") || fileWithoutExt.EndsWith("-man")) {
                    videoList.sessions.Add(video);
                    videoList.sessionsSize += video.size;
                }
                else {
                    videoList.clips.Add(video);
                    videoList.clipsSize += video.size;
                }
            }
            Logger.WriteLine($"Parsed '{videoList.sessions.Count + videoList.clips.Count}' video files. Sessions: {videoList.sessions.Count}, Clips: {videoList.clips.Count}.");

            videoList.games.Sort();

            return videoList;
        }

        public static double GetVideoDuration(string videoPath) {
            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffprobe"),
                Arguments = string.Format("-i \"{0}\" -show_entries format=duration -v quiet -of csv=\"p = 0\"", videoPath),
            };

            var process = new Process {
                StartInfo = startInfo
            };
            process.Start();
            string stdout = "", stderr = "";
            double duration;
            try {
                stdout = process.StandardOutput.ReadToEnd();
                stderr = process.StandardError.ReadToEnd();
                duration = double.Parse(stdout, CultureInfo.InvariantCulture);
            }
            catch (Exception e) {
                // if exception happens, usually means video is not valid?
                Logger.WriteLine($"Issue retrieving duration of video? exception: '{e.Message}'");
                Logger.WriteLine($"arguments: {startInfo.Arguments}");
                Logger.WriteLine($"reason: {stdout + stderr}");
                duration = 0;
            }
            process.WaitForExit();
            process.Close();

            return duration;
        }

        public static string GetOrCreateThumbnail(string videoPath, double duration = 0) {
            string thumbsDir = Path.Combine(Path.GetDirectoryName(videoPath), ".thumbs/");
            string[] thumbnailExtensions = [".jpg", ".webp", ".png"];
            string thumbnailPath = thumbnailExtensions.Select(ext => Path.Combine(thumbsDir, Path.GetFileNameWithoutExtension(videoPath) + ext))
                .FirstOrDefault(File.Exists);

            if (thumbnailPath != null) return thumbnailPath;
            else thumbnailPath = Path.Combine(thumbsDir, Path.GetFileNameWithoutExtension(videoPath) + ".jpg");
            if (!Directory.Exists(thumbsDir)) Directory.CreateDirectory(thumbsDir);

            if (duration == 0) {
                duration = GetVideoDuration(videoPath);
                if (duration == 0) {
                    Logger.WriteLine($"Failed to create thumbnail {thumbnailPath}, details: duration of video is 0, corrupted video?");
                    return thumbnailPath;
                }
            }

            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffmpeg"),
                Arguments = string.Format("-ss {0} -y -i \"{1}\" -vframes 1 -s 1024x576 \"{2}\"",
                    (duration / 2).ToString(CultureInfo.InvariantCulture), videoPath, thumbnailPath),
            };

            var process = new Process {
                StartInfo = startInfo
            };
            process.Start();
            var details = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();

            if (!File.Exists(thumbnailPath)) {
                Logger.WriteLine(startInfo.Arguments);
                Logger.WriteLine($"Failed to create thumbnail {thumbnailPath}, details: {details}");
            }
            else Logger.WriteLine($"Created new thumbnail: {thumbnailPath}");

            process.WaitForExit();
            process.Close();

            return thumbnailPath;
        }

        public static VideoMetadata GetOrCreateMetadata(string videoPath) {
            string thumbsDir = Path.Combine(Path.GetDirectoryName(videoPath), ".thumbs/");
            string metadataPath = Path.Combine(thumbsDir, Path.GetFileNameWithoutExtension(videoPath) + ".metadata");

            if (File.Exists(metadataPath)) {
                try {
                    return JsonSerializer.Deserialize<VideoMetadata>(File.ReadAllText(metadataPath));
                }
                catch (Exception ex) {
                    Logger.WriteLine($"Error deserializing video metadata for '{Path.GetFileName(videoPath)}': {ex.Message}");
                    File.Delete(metadataPath);
                    return GetOrCreateMetadata(videoPath);
                }
            }
            else {
                var metadata = new VideoMetadata();

                if (!Directory.Exists(thumbsDir)) Directory.CreateDirectory(thumbsDir);

                var duration = GetVideoDuration(videoPath);
                if (duration == 0) {
                    Logger.WriteLine($"Failed to create metadata {metadataPath}, details: duration of video is 0, corrupted video?");
                    return metadata;
                }
                metadata.duration = duration;

                Logger.WriteLine($"Created video metadata for '{Path.GetFileName(videoPath)}'");
                File.WriteAllText(metadataPath, JsonSerializer.Serialize<VideoMetadata>(metadata));
                return metadata;
            }
        }

        public static void DeleteVideo(string filePath) {
            var metaPath = Path.Join(Path.GetDirectoryName(filePath), ".thumbs/");
            string[] metaFileExtensions = new string[] { ".png", ".webp", ".metadata" };
            IEnumerable<string> metaFilesToDelete = metaFileExtensions.SelectMany(ext => Directory.GetFiles(metaPath, Path.GetFileNameWithoutExtension(filePath) + ext));

            Logger.WriteLine($"Deleting file: {filePath}");
            File.Delete(filePath);
            foreach (string metaFile in metaFilesToDelete) {
                Logger.WriteLine($"Deleting file: {metaFile}");
                File.Delete(metaFile);
            }
        }

        public static void BackupBookmarks(string videoName, string json) {
            try {
                Logger.WriteLine($"Backing up bookmarks for video: {videoName}");
                string BookmarkBackupFilePath = Path.Join(GetTempFolder(), videoName + "_bookmarks.bak");
                Logger.WriteLine($"Backup file location: {BookmarkBackupFilePath}");
                File.WriteAllText(BookmarkBackupFilePath, json);
            }
            catch (Exception ex) {
                Logger.WriteLine($"Could not backup {videoName}. Exception: {ex.Message}");
            }
        }

        public static void LoadBackupBookmarks() {
            try {
                string[] bookmarkBackupFiles = Directory.GetFiles(GetTempFolder(), "*_bookmarks.bak", SearchOption.TopDirectoryOnly);
                if (bookmarkBackupFiles.Length == 0) return;
                Logger.WriteLine($"Loading {bookmarkBackupFiles.Length} bookmark backups");
                foreach (string bookmarkBackupFile in bookmarkBackupFiles) {
                    Logger.WriteLine($"Loading {bookmarkBackupFile}");
                    string json = File.ReadAllText(bookmarkBackupFile);
                    WebMessage webMessage = new() {
                        message = "SetBookmarks",
                        data = json
                    };
                    WebMessage.SendMessage(JsonSerializer.Serialize(webMessage));
                    File.Delete(bookmarkBackupFile);
                    Logger.WriteLine($"Successfully applied backups for {bookmarkBackupFile}");
                }
            }
            catch (Exception ex) {
                Logger.WriteLine($"Could not load backup bookmarks. Exception: {ex.Message}");
            }
        }

        public static string CreateClip(string game, string videoPath, ClipSegment[] clipSegments, int index = 0, int progress = 0, string uuid = null) {
            string inputFile = Path.Join(GetPlaysFolder(), videoPath).Replace("\\", "/");
            string outputFile = Path.Combine(Path.GetDirectoryName(inputFile), DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "-clp.mp4");

            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffmpeg"),
            };

            if (clipSegments.Length > 1 && index != clipSegments.Length) {
                if (index == 0) File.Delete(Path.Join(GetTempFolder(), "list.txt"));
                outputFile = Path.Join(GetTempFolder(), "temp" + index + ".mp4").Replace("\\", "/");
                File.AppendAllLines(Path.Join(GetTempFolder(), "list.txt"), new[] { "file 'temp" + index + ".mp4'" });
            }

            string codecArgs = "";
            if (SettingsService.Settings.clipSettings.reEncode) {
                codecArgs = $"-c:v {SettingsService.Settings.clipSettings.renderCodec}";
                if (SettingsService.Settings.clipSettings.renderHardware == "GPU") {
                    codecArgs += $" -cq:v {SettingsService.Settings.clipSettings.renderQuality}";
                }
                else {
                    codecArgs += $" -crf {SettingsService.Settings.clipSettings.renderQuality}";
                }

                if (SettingsService.Settings.clipSettings.renderCustomFps.HasValue) {
                    codecArgs += $" -r {SettingsService.Settings.clipSettings.renderCustomFps.Value}";
                }
            }
            else {
                codecArgs = "-c:v copy -c:a copy";
            }

            if (clipSegments.Length > 1 && index == clipSegments.Length) {
                startInfo.Arguments =
                "-v warning -hide_banner -stats " +
                "-f concat -safe 0 " +
                $"-i \"{Path.Join(GetTempFolder(), "list.txt").Replace("\\", "/")}\" " +
                $"{codecArgs} " +
                $"\"{outputFile}\"";
                Logger.WriteLine(startInfo.Arguments);
            }
            else {
                startInfo.Arguments =
                "-v warning -hide_banner -stats " +
                "-ss " + clipSegments[index].start.ToString(CultureInfo.InvariantCulture) + " " +
                "-i \"" + inputFile + "\" " +
                "-t " + clipSegments[index].duration.ToString(CultureInfo.InvariantCulture) + " " +
                $"{codecArgs} " +
                "-avoid_negative_ts make_zero -fflags +genpts -y " +
                $"\"{outputFile}\"";
                Logger.WriteLine(startInfo.Arguments);
            }

            var process = new Process {
                StartInfo = startInfo
            };

            uuid ??= Guid.NewGuid().ToString();

            long totalRenderTime = (long)(clipSegments.Sum(segment => segment.duration) * (clipSegments.Length > 1 ? 2 : 1));
            process.OutputDataReceived += new DataReceivedEventHandler((s, e) => {
                Logger.WriteLine("O: " + e.Data);
            });
            process.ErrorDataReceived += new DataReceivedEventHandler((s, e) => {
                if (e.Data != null && e.Data.Contains("frame=") && e.Data.Contains("speed=") && !e.Data.Contains("Lsize=")) {
                    WebMessage.DisplayToast(uuid, game, "Creating clip", "none", Convert.ToInt32(TimeSpan.Parse(e.Data.Trim().Substring(48, 11)).TotalSeconds) + progress, totalRenderTime);
                }
                Logger.WriteLine("E: " + e.Data);
            });

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.Close();

            var verifyClip = new ProcessStartInfo {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffprobe"),
                Arguments = $"-v error -i \"{outputFile}\""
            };

            using var verifyClipProcess = Process.Start(verifyClip);
            string output = verifyClipProcess.StandardOutput.ReadToEnd();
            Logger.WriteLine("Output: " + output);
            verifyClipProcess.WaitForExit();

            if (!File.Exists(outputFile) || !string.IsNullOrWhiteSpace(output)) {
                WebMessage.DestroyToast(uuid);
                Logger.WriteLine(string.Format("FFMPEG error. Failed to create clip: {0}", outputFile));
                File.Delete(outputFile);
                return null;
            }

            if (clipSegments.Length > 1 && index != clipSegments.Length) return CreateClip(game, videoPath, clipSegments, index + 1, (int)(progress + clipSegments[index].duration), uuid);
            else if (clipSegments.Length > 1 && index == clipSegments.Length) {
                WebMessage.DestroyToast(uuid);
                Logger.WriteLine(string.Format("Created new multiclip: {0}", outputFile));
            }
            else {
                WebMessage.DestroyToast(uuid);
                Logger.WriteLine(string.Format("Created new clip: {0}", outputFile));
            }

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
                file.CopyTo(tempPath, true);
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
#if WINDOWS
            return Convert.ToBase64String(
                ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(stringToEncrypt)
                    , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                    , DataProtectionScope.CurrentUser));
#else
            return stringToEncrypt;
#endif
        }

        public static string DecryptString(string encryptedString, string optionalEntropy = null) {
#if WINDOWS
            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedString)
                    , optionalEntropy != null ? Encoding.UTF8.GetBytes(optionalEntropy) : null
                    , DataProtectionScope.CurrentUser));
#else
            return encryptedString;
#endif
        }

        public static string GetReadableFileSize(double bytes) {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1) {
                order++;
                bytes = bytes / 1024;
            }

            return string.Format("{0:0.##} {1}", bytes, sizes[order]);
        }

        public static string GetAspectRatio(int width, int height) {
            BigInteger gcd = BigInteger.GreatestCommonDivisor(width, height);
            if (gcd == 0)
                return $"0:0";

            int aspectWidth = width / (int)gcd;
            int aspectHeight = height / (int)gcd;
            return $"{aspectWidth}:{aspectHeight}";
        }

        public static bool IsValidAspectRatio(int width, int height) {
            return new[] { "64:27", "43:18", "21:9", "16:10", "16:9", "4:3", "32:9" }.Contains(GetAspectRatio(width, height));
        }

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source) {
            return source.Select((item, index) => (item, index));
        }

        public static float CalculateStringSimilarity(string path1, string path2) {
            int maxLength = Math.Max(path1.Length, path2.Length);
            int distance = GetDamerauLevenshteinDistance(path1, path2);
            float similarity = 1 - (float)distance / maxLength;

            return similarity;
        }

        public static int GetDamerauLevenshteinDistance(string s, string t) {
            int n = s.Length; // length of s
            int m = t.Length; // length of t

            if (n == 0) {
                return m;
            }

            if (m == 0) {
                return n;
            }

            int[] p = new int[n + 1]; //'previous' cost array, horizontally
            int[] d = new int[n + 1]; // cost array, horizontally

            // indexes into strings s and t
            int i; // iterates through s
            int j; // iterates through t

            for (i = 0; i <= n; i++) {
                p[i] = i;
            }

            for (j = 1; j <= m; j++) {
                char tJ = t[j - 1]; // jth character of t
                d[0] = j;

                for (i = 1; i <= n; i++) {
                    int cost = s[i - 1] == tJ ? 0 : 1; // cost
                    // minimum of cell to the left+1, to the top+1, diagonally left and up +cost                
                    d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1] + cost);
                }

                // copy current distance counts to 'previous row' distance counts
                int[] dPlaceholder = p; //placeholder to assist in swapping p and d
                p = d;
                d = dPlaceholder;
            }

            // our last action in the above loop was to switch d and p, so p now 
            // actually has the most recent cost counts
            return p[n];
        }

        public static string? GetGpuManufacturer() {
#if !WINDOWS
                try {
                    ProcessStartInfo psi = new ProcessStartInfo {
                        FileName = "lspci",
                        Arguments = "-nn | grep VGA",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (Process proc = new Process { StartInfo = psi }) {
                        proc.Start();
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                        if (output.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) {
                            return "NVIDIA";
                        }
                        else if (output.Contains("AMD", StringComparison.OrdinalIgnoreCase) || output.Contains("ATI", StringComparison.OrdinalIgnoreCase)) {
                            return "AMD";
                        }
                        else if (output.Contains("Intel", StringComparison.OrdinalIgnoreCase)) {
                            return "Intel";
                        }
                    }
                }
                catch (Exception ex) {
                    Logger.WriteLine($"Error detecting GPU type on Linux: {ex.Message}");
                }
                return null;
#else
            try {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController")) {
                    foreach (var obj in searcher.Get()) {
                        string name = obj["Name"]?.ToString() ?? string.Empty;
                        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) {
                            return "NVIDIA";
                        }
                        else if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("ATI", StringComparison.OrdinalIgnoreCase)) {
                            return "AMD";
                        }
                        else if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase)) {
                            return "Intel";
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logger.WriteLine($"Error detecting GPU type: {ex.Message}");
            }
            return null;
#endif
        }

        private static int elapsedSeconds;
        private static Timer checkForNvidiaUpdateTimer;

        public static async void DownloadNvidiaAudioSDK() {
            var query = new ObjectQuery("SELECT * FROM Win32_VideoController");
            var searcher = new ManagementObjectSearcher(query);

            // Regex pattern to extract the graphics card version (Example: 3060, 4090 TI, 2070)
            Regex regex = new Regex(@"\b[2-4]0[0-9]0\b");
            string gpuRTX = null;

            // Retrieve and display the graphics card information
            foreach (ManagementObject obj in searcher.Get()) {
                string gpuName = obj["Name"].ToString();
                Logger.WriteLine("Detected GPU: " + gpuName);
                Match match = regex.Match(gpuName);

                if (match.Success) {
                    gpuRTX = match.Value;
                }
            }

            if (gpuRTX == null) {
                WebMessage.DisplayModal("You must have an RTX graphics card to use NVIDIA Noise Removal", "Warning", "warning");
                return;
            }

            int versionNumber = int.Parse(gpuRTX[..2]);
            string url;

            switch (versionNumber) {
                case 20:
                    url = "https://international.download.nvidia.com/Windows/broadcast/sdk/AFX/2022-12-22_nvidia_afx_sdk_win_v1.3.0.21_turing.exe";
                    break;
                case 30:
                    url = "https://international.download.nvidia.com/Windows/broadcast/sdk/AFX/2022-12-22_nvidia_afx_sdk_win_v1.3.0.21_ampere.exe";
                    break;
                case 40:
                    url = "https://international.download.nvidia.com/Windows/broadcast/sdk/AFX/2022-12-22_nvidia_afx_sdk_win_v1.3.0.21_ada.exe";
                    break;
                default:
                    WebMessage.DisplayModal("You must have an RTX graphics card to use NVIDIA Noise Removal", "Warning", "warning");
                    return;
            }

            string savePath = Path.Join(GetTempFolder(), "nvidia.exe");

            using (HttpClient client = new HttpClient()) {
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)) {
                    response.EnsureSuccessStatusCode();

                    long? totalBytes = response.Content.Headers.ContentLength;

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync()) {
                        await ProcessContentStream(contentStream, totalBytes, savePath);
                    }
                }
            }

            WebMessage.DestroyToast("Nvidia");
            Logger.WriteLine("Download completed!");

            ProcessStartInfo startInfo = new ProcessStartInfo(savePath) {
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(startInfo);

            elapsedSeconds = 0;
            checkForNvidiaUpdateTimer = new Timer(1000);
            checkForNvidiaUpdateTimer.Elapsed += TimerElapsed;
            checkForNvidiaUpdateTimer.AutoReset = true;
            checkForNvidiaUpdateTimer.Start();
        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e) {
            elapsedSeconds++;

            LibObsRecorder activeRecorder = (LibObsRecorder)RecordingService.ActiveRecorder;
            if (activeRecorder.HasNvidiaAudioSDK()) {
                checkForNvidiaUpdateTimer.Stop();
                WebMessage.SendMessage(GetUserSettings());
            }

            const int maxSeconds = 600;
            if (elapsedSeconds >= maxSeconds) {
                checkForNvidiaUpdateTimer.Stop();
            }
        }

        public static string GetGitSHA1Hash(string filePath) {
            var hash = "";
            try {
                using var sha1 = SHA1.Create();
                using var stream = File.OpenRead(filePath);
                byte[] contentBytes = Encoding.ASCII.GetBytes($"blob {stream.Length}\0");
                sha1.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);

                byte[] fileBytes = new byte[4096];
                int bytesRead;
                while ((bytesRead = stream.Read(fileBytes, 0, fileBytes.Length)) > 0) {
                    sha1.TransformBlock(fileBytes, 0, bytesRead, fileBytes, 0);
                }
                sha1.TransformFinalBlock(fileBytes, 0, 0);
                hash = BitConverter.ToString(sha1.Hash).Replace("-", "").ToLower();
            }
            catch (Exception ex) {
                Logger.WriteLine($"Error retrieving sha1 file hash: {ex.Message}");
            }
            return hash;
        }

        static async Task ProcessContentStream(Stream contentStream, long? totalBytes, string savePath) {
            long totalDownloaded = 0;
            byte[] buffer = new byte[8192];
            int bytesRead;

            using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true)) {
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalDownloaded += bytesRead;

                    if (totalBytes.HasValue) {
                        int progressPercentage = (int)((totalDownloaded * 100) / totalBytes.Value);
                        WebMessage.DisplayToast("Nvidia", "Nvidia Audio SDK", "Downloading", "none", progressPercentage, 100);
                    }
                }
            }
        }
    }
}
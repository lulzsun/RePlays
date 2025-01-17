using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;


#if !WINDOWS
using RePlays.Recorders;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
#endif
using static RePlays.Utils.Functions;

namespace RePlays.Services {
    public static class DetectionService {
        static JsonElement[] gameDetectionsJson;
        static JsonElement[] nonGameDetectionsJson;
        static readonly HashSet<string> nonGameDetectionsCache = [];
        static readonly string gameDetectionsFile = Path.Join(GetCfgFolder(), "gameDetections.json");
        static readonly string nonGameDetectionsFile = Path.Join(GetCfgFolder(), "nonGameDetections.json");
        private static List<string> classBlacklist = ["plasmashell", "splashscreen", "splashwindow", "launcher", "cheat", "console"];
        private static List<string> classWhitelist = ["steam_app_", "unitywndclass", "unrealwindow", "riotwindowclass"];

        public static void Start() {
            Logger.WriteLine("DetectionService starting...");
            LoadDetections();
            WindowService.Start();
        }

        public static void Stop() {
            Logger.WriteLine("DetectionService stopping...");
            WindowService.Stop();
        }

        public static void WindowCreation(IntPtr hwnd, int processId = 0, [CallerMemberName] string memberName = "") {
            if (RecordingService.IsRecording)
                return;

            if (processId == 0 && hwnd != 0)
                WindowService.GetWindowThreadProcessId(hwnd, out processId);
            else if (processId == 0 && hwnd == 0)
                return;

            WindowService.GetExecutablePathFromProcessId(processId, out string executablePath);

            if (executablePath != null) {
                if (executablePath.ToLower().StartsWith(@"c:\windows\")) {   // if this program is starting from here,
                    return;                                                             // we can assume it is not a game
                }
            }
            if (processId != 0 && AutoDetectGame(processId, executablePath, hwnd)) {
                Logger.WriteLine($"WindowCreation: [{processId}][{hwnd}][{executablePath}]", memberName: memberName);
            }
        }

        public static void WindowDeletion(IntPtr hwnd, int processId = 0, [CallerMemberName] string memberName = "") {
            if (!RecordingService.IsRecording || RecordingService.IsStopping)
                return;

            if (processId == 0 && hwnd != 0)
                WindowService.GetWindowThreadProcessId(hwnd, out processId);
            else if (processId == 0 && hwnd == 0)
                return;

            WindowService.GetExecutablePathFromProcessId(processId, out string executablePath);
            var currentSession = RecordingService.GetCurrentSession();

            if (currentSession.Pid != 0 && (currentSession.Pid == processId || currentSession.WindowHandle == hwnd)) {
                try {
                    var process = Process.GetProcessById(currentSession.Pid);
                    if (process.HasExited) throw new Exception();
                }
                catch (Exception) {
                    // Process no longer exists, must be safe to end recording(?)
                    if (processId == 0) processId = currentSession.Pid;
                    if (executablePath == "") executablePath = currentSession.Exe;
                    Logger.WriteLine($"WindowDeletion: [{processId}][{hwnd}][{executablePath}]", memberName: memberName);
                    RecordingService.StopRecording();
                }
            }
        }

        public static void CheckTopLevelWindows() {
            var windows = WindowService.GetTopLevelWindows();
            foreach (IntPtr window in windows) {
                WindowCreation(window);
            }
        }

        public static void LoadDetections() {
            gameDetectionsJson = DownloadDetections(gameDetectionsFile, "gameDetections.json");
            nonGameDetectionsJson = DownloadDetections(nonGameDetectionsFile, "nonGameDetections.json");
            LoadNonGameCache();
        }

        public static JsonElement[] DownloadDetections(string dlPath, string file) {
            var result = "[]";

#if DEBUG
            dlPath = Path.Join(GetSolutionPath(), @"Resources/detections/", file);
            Logger.WriteLine($"Debug: Using {file} from Resources folder instead.");
            return JsonDocument.Parse(File.ReadAllText(dlPath)).RootElement.EnumerateArray().ToArray();
#endif
            try {
                // check if current file sha matches remote or not, if it does, we are already up-to-date
                if (File.Exists(dlPath)) {
                    var hash = GetGitSHA1Hash(dlPath);
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "RePlays Client");
                    var getTask = httpClient.GetAsync("https://api.github.com/repos/lulzsun/RePlays/contents/Resources/detections/" + file);
                    getTask.Wait();
                    if (hash != "" && getTask.Result.Headers.ETag != null && getTask.Result.Headers.ETag.ToString().Contains(hash)) {
                        return JsonDocument.Parse(File.ReadAllText(dlPath)).RootElement.EnumerateArray().ToArray();
                    }
                }
                // download detections and verify hash
                using (var httpClient = new HttpClient()) {
                    var getTask = httpClient.GetStringAsync("https://raw.githubusercontent.com/lulzsun/RePlays/main/Resources/detections/" + file);
                    getTask.Wait();
                    result = getTask.Result;
                }
                File.WriteAllText(dlPath, result);
                Logger.WriteLine($"Downloaded {file} sha1={GetGitSHA1Hash(dlPath)}");
            }
            catch (Exception e) {
                Logger.WriteLine($"Unable to download detections: {file}. Error: {e.Message}");
                if (File.Exists(dlPath)) {
                    return JsonDocument.Parse(File.ReadAllText(dlPath)).RootElement.EnumerateArray().ToArray();
                }
            }
            return JsonDocument.Parse(result).RootElement.EnumerateArray().ToArray();
        }

        /// <summary>
        /// <para>Checks to see if the process:</para>
        /// <para>1. contains in the game detection list (whitelist)</para>
        /// <para>2. does NOT contain in nongame detection list (blacklist)</para>
        /// <para>3. contains any graphics dll modules (directx, opengl)</para>
        /// <para>If 2 and 3 are true, we will also assume it is a "game"</para>
        /// </summary>
        public static bool AutoDetectGame(int processId, string executablePath, nint windowHandle = 0, bool autoRecord = true) {
#if !WINDOWS
            // If process is launching as a wine executable
            if (processId != 0 && Regex.IsMatch(executablePath, @".*(?:/wine64-preloader|/wine-preloader)$")) {
                string cmdLineArgs = File.ReadAllText($"/proc/{processId}/cmdline").Replace('\0', ' ');
                // Retrieve .exe path from cmdLineArgs
                Match match = Regex.Match(cmdLineArgs, @"[A-Za-z]:[\\\/](.+\.exe)");
                if (match.Success)
                    executablePath = ("\\" + match.Groups[1].Value).Replace("\\", "/");
                else
                    Logger.WriteLine($"Could not extract .exe path from cmdLineArgs: {cmdLineArgs}");
            }
#endif
            if (processId == 0 || (executablePath != "" && IsMatchedNonGame(executablePath))) {
                if (processId == 0)
                    Logger.WriteLine($"Process id should never be zero here, developer error?");
#if DEBUG
                else
                    Logger.WriteLine($"Blacklisted application: [{processId}][{executablePath}]");
#endif
                return false;
            }
            var gameDetection = IsMatchedGame(executablePath);

            if (!gameDetection.isGame && SettingsService.Settings.captureSettings.recordingMode == "whitelist") { return false; }

            // If the windowHandle we captured is problematic, just return nothing
            // Problematic handles are created if the application for example,
            // the game displays a splash screen (SplashScreenClass) before launching
            // This detection is very primative and only covers specific cases, in the future we should find another way
            // to approach this issue. (possibily fetch to see if the window size ratio is not standard?)
            if (windowHandle <= 0) windowHandle = WindowService.GetWindowHandleByProcessId(processId, true);
            var className = WindowService.GetClassName(windowHandle);
            string gameTitle = gameDetection.gameTitle;
            string fileName = Path.GetFileName(executablePath);
            var detailedWindowStr = $"[{processId}][{windowHandle}][{className}][{executablePath}]";
            try {
                if (Path.Exists(executablePath)) {
                    FileVersionInfo fileInformation = FileVersionInfo.GetVersionInfo(executablePath);
                    bool hasBadWordInDescription = fileInformation.FileDescription != null ? classBlacklist.Where(c => fileInformation.FileDescription.ToLower().Contains(c)).Any() : false;
                    bool hasBadWordInClassName = classBlacklist.Where(c => className.ToLower().Contains(c)).Any() || classBlacklist.Where(c => className.ToLower().Replace(" ", "").Contains(c)).Any();
                    bool hasBadWordInGameTitle = classBlacklist.Where(c => gameTitle.ToLower().Contains(c)).Any() || classBlacklist.Where(c => gameTitle.ToLower().Replace(" ", "").Contains(c)).Any();
                    bool hasBadWordInFileName = classBlacklist.Where(c => fileName.ToLower().Contains(c)).Any() || classBlacklist.Where(c => fileName.ToLower().Replace(" ", "").Contains(c)).Any();

                    bool isBlocked = hasBadWordInDescription || hasBadWordInClassName || hasBadWordInGameTitle || hasBadWordInFileName;
                    if (isBlocked) {
                        Logger.WriteLine($"Blocked application: {detailedWindowStr}");
                        return false;
                    }
                }
            }
            catch (Exception e) {
                Logger.WriteLine($"Failed to check blacklist for application: {executablePath} with error message: {e.Message}");
            }

            if (!autoRecord) {
                // This is a manual/forced record event so lets just yolo it and assume user knows best
                RecordingService.SetCurrentSession(processId, windowHandle, gameTitle, executablePath);
                Logger.WriteLine($"Forced record start: {detailedWindowStr}, prepared to record");
                return true;
            }

            bool isGame = gameDetection.isGame;
            var windowSize = WindowService.GetWindowSize(windowHandle);
            var aspectRatio = GetAspectRatio(windowSize.GetWidth(), windowSize.GetHeight());
            bool isValidAspectRatio = IsValidAspectRatio(windowSize.GetWidth(), windowSize.GetHeight());
            bool isWhitelistedClass = classWhitelist.Where(c => className.ToLower().Contains(c)).Any() || classWhitelist.Where(c => className.ToLower().Replace(" ", "").Contains(c)).Any();
            detailedWindowStr += $"[{windowSize}, {aspectRatio}]";

            // if there is no matched game, lets try to make assumptions from the process given the following information:
            // 1. window size & aspect ratio
            // 2. window class name (matches against whitelist)
            // if all conditions are true, then we can assume it is a game
            if (!isGame) {
                if (windowSize.GetWidth() <= 69 || windowSize.GetHeight() <= 69) {
                    return false;
                }
                if (isWhitelistedClass && isValidAspectRatio) {
                    Logger.WriteLine($"Assumed recordable game: {detailedWindowStr}");
                    isGame = true;
                }
                else {
                    Logger.WriteLine($"Unknown application: {detailedWindowStr}");
                }
            }
            if (isGame) {
                // TODO: check obs source windows for a match
                if (!isValidAspectRatio) {
                    // ignore bad window size if game is user whitelisted
                    bool isUserWhitelisted = SettingsService.Settings.detectionSettings.whitelist.Any(
                        game => string.Equals(game.gameExe.ToLower(), executablePath.ToLower())
                    );
#if !WINDOWS
                    // linux (at least proton-based) games don't have a different classname for their splashscreen.
                    // we need do check other things to see if it is a splashscreen before we record.
                    // a common pattern is that these splashscreens have a different window title than the game.
                    // i.e, untitled splashscreen
                    if (WindowService.GetWindowTitle(windowHandle) == "") {
                        Logger.WriteLine($"Found splashscreen window {detailedWindowStr}, ignoring start capture.");
                        return false;
                    }
#endif
                    Logger.WriteLine($"Found game window {detailedWindowStr}, but invalid resolution, " +
                        (!isWhitelistedClass && !isUserWhitelisted ? $"ignoring start capture." : "not ignoring due to whitelist.")
                    );
                    if (!isWhitelistedClass && !isUserWhitelisted) return false;
                }
                bool allowed = SettingsService.Settings.captureSettings.recordingMode is "automatic" or "whitelist";
                Logger.WriteLine($"{(allowed ? "Starting capture for" : "Ready to capture")} application: {detailedWindowStr}");
                RecordingService.SetCurrentSession(processId, windowHandle, gameTitle, executablePath, gameDetection.forceDisplayCapture);
                if (allowed) RecordingService.StartRecording(false);
            }
            return isGame;
        }

        public static bool HasBadWordInClassName(IntPtr windowHandle) {
            var className = WindowService.GetClassName(windowHandle);
            bool hasBadWordInClassName = classBlacklist.Any(c => className.ToLower().Contains(c)) || classBlacklist.Any(c => className.ToLower().Replace(" ", "").Contains(c));
            if (hasBadWordInClassName) windowHandle = IntPtr.Zero;
            return windowHandle == IntPtr.Zero;
        }

        public static (bool isGame, bool forceDisplayCapture, string gameTitle) IsMatchedGame(string exeFile) {
            foreach (var game in SettingsService.Settings.detectionSettings.whitelist) {
                if (string.Equals(game.gameExe.ToLower(), exeFile.ToLower())) return (true, false, game.gameName);
            }
            if (SettingsService.Settings.captureSettings.recordingMode == "whitelist") return (false, false, "Whitelist Mode");

            try {
                for (int x = 0; x < gameDetectionsJson.Length; x++) {
                    JsonElement[] gameDetections = gameDetectionsJson[x].GetProperty("game_detection").EnumerateArray().ToArray();

                    for (int y = 0; y < gameDetections.Length; y++) {
                        bool d1 = gameDetections[y].TryGetProperty("gameexe", out JsonElement detection1);
                        string exePattern = "";

                        if (d1) {
                            exePattern = detection1.GetString();
                        }

                        if (exePattern != null && exePattern.Length > 0) {
                            exeFile = exeFile.Replace("\\", "/");
                            // if the exeFile passed was not a fullpath
                            if (exeFile == Path.GetFileName(exeFile)) {
                                var exePatterns = exePattern.Split('|');
                                for (int z = 0; z < exePatterns.Length; z++) {
                                    exePattern = exePatterns[z].Split('/').Last();
                                    if (exePatterns[z].Length > 0 && Regex.IsMatch(exeFile, "^" + exePattern + "$", RegexOptions.IgnoreCase)) {
                                        Logger.WriteLine($"Regex Matched: input=\"{exeFile}\", pattern=\"^{exePattern}\"$");
                                        return (true, HasForcedDisplayCapture(gameDetections[y]), gameDetectionsJson[x].GetProperty("title").ToString());
                                    }
                                }
                            }
                            else {
                                if (Regex.IsMatch(exeFile, exePattern, RegexOptions.IgnoreCase)) {
                                    Logger.WriteLine($"Regex Matched: input=\"{exeFile}\", pattern=\"{exePattern}\"");
                                    return (true, HasForcedDisplayCapture(gameDetections[y]), gameDetectionsJson[x].GetProperty("title").ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logger.WriteLine($"Exception occurred during gameDetections.json parsing: {ex.Message}");
            }

            // TODO: also parse Epic games/Origin games
            if (exeFile.Replace("\\", "/").Contains("/steamapps/common/")) {
                string gameName = GetNameForSteamGame(exeFile);
                if (!string.IsNullOrEmpty(gameName))
                    return (true, false, gameName);
            }
            return (false, false, "Game Unknown");
        }

        public static bool HasForcedDisplayCapture(JsonElement matchedGame) {
            return matchedGame.TryGetProperty("force_display_capture", out JsonElement prop)
                                            ? prop.GetBoolean()
                                            : false;
        }

        public static bool IsMatchedNonGame(string executablePath) {
            if (executablePath is null) return false;
            executablePath = executablePath.ToLower();

            // if this exe is in the user whitelist, then it should not be a non-game
            foreach (var game in SettingsService.Settings.detectionSettings.whitelist) {
                // we are only checking for fileName here, which is a bad idea
                // TODO: do proper full path check (also fix IsMatchedGame() to do the same)
                if (game.gameExe.ToLower() == Path.GetFileName(executablePath)) return false;
            }

            foreach (var path in SettingsService.Settings.detectionSettings.blacklist) {
                float result = CalculateStringSimilarity(path.ToLower(), executablePath);
                if (result > 0.75) return true;
            }

            if (nonGameDetectionsCache.Contains(Path.GetFileName(executablePath))) {
                return true;
            }

            return false;
        }

        private static string GetNameForSteamGame(string exeFile) {
            try {
                string normalizedPath = exeFile.Replace("\\", "/");
                var splitPath = Regex.Split(normalizedPath, "/steamapps/common/", RegexOptions.IgnoreCase);

                string installDir = splitPath[1].Split('/')[0];
                string steamAppsDir = Path.Combine(Path.GetDirectoryName(splitPath[0]), "Steam/steamapps");

                if (!Directory.Exists(steamAppsDir))
                    return Regex.Split(exeFile.Replace("\\", "/"), "/steamapps/common/", RegexOptions.IgnoreCase)[1].Split('/')[0];

                foreach (string acfFile in Directory.GetFiles(steamAppsDir, "*.acf")) {
                    string content = File.ReadAllText(acfFile);
                    string acfInstalldir = ExtractAcfValue(content, "installdir");
                    string gameName = ExtractAcfValue(content, "name");

                    // If the acfInstalldir matches installDir, then we know that it's the correct file and we can return the value in gameName
                    if (string.Equals(acfInstalldir, installDir, StringComparison.OrdinalIgnoreCase)) {
                        return gameName;
                    }
                }
                return Regex.Split(exeFile.Replace("\\", "/"), "/steamapps/common/", RegexOptions.IgnoreCase)[1].Split('/')[0];
            }
            catch {
                return null;
            }
        }

        private static string ExtractAcfValue(string content, string key) {
            var match = Regex.Match(content, $"\"{key}\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static void LoadNonGameCache() {
            nonGameDetectionsCache.Clear();
            try {
                foreach (JsonElement nonGameDetection in nonGameDetectionsJson) {
                    JsonElement[] detections = nonGameDetection.GetProperty("detections").EnumerateArray().ToArray();

                    //Each "non-game" can have multiple .exe-files
                    foreach (JsonElement detection in detections) {
                        //Get the exe filename
                        if (detection.TryGetProperty("detect_exe", out JsonElement detectExe)) {
                            string[] jsonExePaths = detectExe.GetString().ToLower().Split('|');

                            foreach (string jsonExePath in jsonExePaths) {
                                nonGameDetectionsCache.Add(Path.GetFileName(jsonExePath));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logger.WriteLine($"Exception occurred during nonGameDetections.json parsing: {ex.Message}");
            }
        }
    }
}
using RePlays.Recorders;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

#if WINDOWS
using System.Security;
using System.Security.Cryptography;
using System.Runtime.ConstrainedExecution;
using System.Management;
using System.Windows.Forms;
#endif
using static RePlays.Utils.Functions;

namespace RePlays.Services {
    public static class DetectionService {
        static JsonElement[] gameDetectionsJson;
        static JsonElement[] nonGameDetectionsJson;
        static readonly HashSet<string> nonGameDetectionsCache = [];
        static readonly string gameDetectionsFile = Path.Join(GetCfgFolder(), "gameDetections.json");
        static readonly string nonGameDetectionsFile = Path.Join(GetCfgFolder(), "nonGameDetections.json");
        private static List<string> classBlacklist = ["splashscreen", "launcher", "cheat", "console"];
        private static List<string> classWhitelist = ["unitywndclass", "unrealwindow", "riotwindowclass"];

        public static void Start() {
            LoadDetections();
            WindowService.Start();
        }

        public static void Stop() {
            WindowService.Stop();
        }

        public static void WindowCreation(IntPtr hwnd, uint processId = 0, [CallerMemberName] string memberName = "") {
            if (processId == 0 && hwnd != 0)
                WindowService.GetWindowThreadProcessId(hwnd, out processId);
            else if (processId == 0 && hwnd == 0)
                return;

            WindowService.GetExecutablePathFromProcessId(processId, out string executablePath);

            if (executablePath != null) {
                if (executablePath.ToString().ToLower().StartsWith(@"c:\windows\")) {   // if this program is starting from here,
                    return;                                                             // we can assume it is not a game
                }
            }
            if (processId != 0 && AutoDetectGame((int)processId, executablePath, hwnd)) {
                Logger.WriteLine($"WindowCreation: [{processId}][{hwnd}][{executablePath}]", memberName: memberName);
            }
        }

        public static void WindowDeletion(IntPtr hwnd, uint processId = 0, [CallerMemberName] string memberName = "") {
            if (!RecordingService.IsRecording)
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
                    if (processId == 0) processId = (uint)currentSession.Pid;
                    if (executablePath == "") executablePath = currentSession.Exe;
                    Logger.WriteLine($"WindowDeletion: [{processId}][{hwnd}][{executablePath}]", memberName: memberName);
                    RecordingService.StopRecording();
                }
            }
        }

        public static void CheckAlreadyRunningPrograms() {
#if WINDOWS
            List<IntPtr> windowHandles = new();

            EnumWindows((hWnd, lParam) => {
                GCHandle handle = GCHandle.FromIntPtr(lParam);
                List<IntPtr> handles = (List<IntPtr>)handle.Target;
                handles.Add(hWnd);
                return true;
            }, GCHandle.ToIntPtr(GCHandle.Alloc(windowHandles)));

            foreach (IntPtr handle in windowHandles) {
                WindowCreation(handle);
            }
#endif
        }

        public static void LoadDetections() {
            gameDetectionsJson = DownloadDetections(gameDetectionsFile, "gameDetections.json");
            nonGameDetectionsJson = DownloadDetections(nonGameDetectionsFile, "nonGameDetections.json");
            LoadNonGameCache();
        }

        public static JsonElement[] DownloadDetections(string dlPath, string file) {
            var result = "[]";

#if DEBUG
            dlPath = Path.Join(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, @"Resources/detections/", file);
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
#if DEBUG
                dlPath = Path.Join(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, @"Resources/detections/", file);
                Logger.WriteLine($"Debug: Using {file} from Resources folder instead.");
#endif
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
            if (processId == 0 || (executablePath != "" && IsMatchedNonGame(executablePath))) {
                if (processId == 0)
                    Logger.WriteLine($"Process id should never be zero here, developer error?");
                //else
                //Logger.WriteLine($"Blacklisted application: [{processId}][{executablePath}]");
                return false;
            }
            var gameDetection = IsMatchedGame(executablePath);

            // If the windowHandle we captured is problematic, just return nothing
            // Problematic handles are created if the application for example,
            // the game displays a splash screen (SplashScreenClass) before launching
            // This detection is very primative and only covers specific cases, in the future we should find another way
            // to approach this issue. (possibily fetch to see if the window size ratio is not standard?)
            if (windowHandle == 0) windowHandle = WindowService.GetWindowHandleByProcessId(processId, true);
            var className = WindowService.GetClassName(windowHandle);
            string gameTitle = gameDetection.gameTitle;
            string fileName = Path.GetFileName(executablePath);
            try {
                if (!Path.Exists(executablePath)) return false;
                FileVersionInfo fileInformation = FileVersionInfo.GetVersionInfo(executablePath);
                bool hasBadWordInDescription = fileInformation.FileDescription != null ? classBlacklist.Where(c => fileInformation.FileDescription.ToLower().Contains(c)).Any() : false;
                bool hasBadWordInClassName = classBlacklist.Where(c => className.ToLower().Contains(c)).Any() || classBlacklist.Where(c => className.ToLower().Replace(" ", "").Contains(c)).Any();
                bool hasBadWordInGameTitle = classBlacklist.Where(c => gameTitle.ToLower().Contains(c)).Any() || classBlacklist.Where(c => gameTitle.ToLower().Replace(" ", "").Contains(c)).Any();
                bool hasBadWordInFileName = classBlacklist.Where(c => fileName.ToLower().Contains(c)).Any() || classBlacklist.Where(c => fileName.ToLower().Replace(" ", "").Contains(c)).Any();

                bool isBlocked = hasBadWordInDescription || hasBadWordInClassName || hasBadWordInGameTitle || hasBadWordInFileName;
                if (isBlocked) {
                    Logger.WriteLine($"Blocked application: [{processId}][{windowHandle}][{className}][{executablePath}]");
                    return false;
                }
            }
            catch (Exception e) {
                Logger.WriteLine($"Failed to check blacklist for application: {executablePath} with error message: {e.Message}");
            }

            if (!autoRecord) {
                // This is a manual/forced record event so lets just yolo it and assume user knows best
                RecordingService.SetCurrentSession(processId, windowHandle, gameTitle, executablePath);
                Logger.WriteLine($"Forced record start: [{processId}][{windowHandle}][{className}][{executablePath}], prepared to record");
                return true;
            }

            bool isGame = gameDetection.isGame;
            var windowSize = WindowService.GetWindowSize(windowHandle);
            var aspectRatio = GetAspectRatio(windowSize.GetWidth(), windowSize.GetHeight());
            bool isValidAspectRatio = IsValidAspectRatio(windowSize.GetWidth(), windowSize.GetHeight());
            bool isWhitelistedClass = classWhitelist.Where(c => className.ToLower().Contains(c)).Any() || classWhitelist.Where(c => className.ToLower().Replace(" ", "").Contains(c)).Any();

            // if there is no matched game, lets try to make assumptions from the process given the following information:
            // 1. window size & aspect ratio
            // 2. window class name (matches against whitelist)
            // if all conditions are true, then we can assume it is a game
            if (!isGame) {
                if (windowSize.GetWidth() <= 69 || windowSize.GetHeight() <= 69) {
                    return false;
                }
                if (isWhitelistedClass && isValidAspectRatio) {
                    Logger.WriteLine($"Assumed recordable game: [{processId}]" +
                        $"[{windowHandle}]" +
                        $"[{className}]" +
                        $"[{windowSize.GetWidth()}x{windowSize.GetHeight()}, {aspectRatio}]" +
                        $"[{executablePath}]"
                    );
                    isGame = true;
                }
                else {
                    Logger.WriteLine($"Unknown application: [{processId}]" +
                        $"[{windowHandle}]" +
                        $"[{className}]" +
                        $"[{windowSize.GetWidth()}x{windowSize.GetHeight()}, {aspectRatio}]" +
                        $"[{executablePath}]"
                    );
                }
            }
            if (isGame) {
                if (!isValidAspectRatio) {
                    Logger.WriteLine($"Found game window " +
                        $"[{processId}]" +
                        $"[{windowHandle}]" +
                        $"[{className}]" +
                        $"[{executablePath}], " +
                        $"but invalid resolution [{windowSize.GetWidth()}x{windowSize.GetHeight()}, {aspectRatio}], " +
                        (!isWhitelistedClass ? $"ignoring start capture." : "not ignoring due to whitelisted classname.")
                    );
                    if (!isWhitelistedClass) return false;
                }
                bool allowed = SettingsService.Settings.captureSettings.recordingMode is "automatic" or "whitelist";
                Logger.WriteLine($"{(allowed ? "Starting capture for" : "Ready to capture")} application: " +
                    $"[{processId}]" +
                    $"[{windowHandle}]" +
                    $"[{className}]" +
                    $"[{executablePath}]"
                );
                bool forceDisplayCapture = gameDetection.forceDisplayCapture;
                RecordingService.SetCurrentSession(processId, windowHandle, gameTitle, executablePath, forceDisplayCapture);
                if (allowed) RecordingService.StartRecording();
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
                if (game.gameExe == exeFile) return (true, false, game.gameName);
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
                                        return (true, HasForcedDisplayCapture(gameDetectionsJson[x]), gameDetectionsJson[x].GetProperty("title").ToString());
                                    }
                                }
                            }
                            else {
                                if (Regex.IsMatch(exeFile, exePattern, RegexOptions.IgnoreCase)) {
                                    Logger.WriteLine($"Regex Matched: input=\"{exeFile}\", pattern=\"{exePattern}\"");
                                    return (true, HasForcedDisplayCapture(gameDetectionsJson[x]), gameDetectionsJson[x].GetProperty("title").ToString());
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
            if (exeFile.Replace("\\", "/").Contains("/steamapps/common/"))
                return (true, false, Regex.Split(exeFile.Replace("\\", "/"), "/steamapps/common/", RegexOptions.IgnoreCase)[1].Split('/')[0]);
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
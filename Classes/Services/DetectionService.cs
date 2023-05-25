using RePlays.Recorders;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static RePlays.Utils.Functions;

namespace RePlays.Services {
    public static class DetectionService {
        static MessageWindow messageWindow;
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        static WinEventProc dele; // Keep delegate alive as long as class is alive.
        static IntPtr winActiveHook = IntPtr.Zero;

        static JsonElement[] gameDetectionsJson;
        static JsonElement[] nonGameDetectionsJson;
        static readonly HashSet<string> nonGameDetectionsCache = new();
        static readonly string gameDetectionsFile = Path.Join(GetCfgFolder(), "gameDetections.json");
        static readonly string nonGameDetectionsFile = Path.Join(GetCfgFolder(), "nonGameDetections.json");
        private static Dictionary<string, string> drivePaths = new();
        private static List<string> classBlacklist = new() { "splashscreen", "launcher", "cheat", "sdl_app", "console" };
        private static List<string> classWhitelist = new() { "unitywndclass" };
        public static bool IsStarted { get; internal set; }

        public static void Start() {
            LoadDetections();

            // Get device paths for mounted drive letters
            for (char letter = 'A'; letter <= 'Z'; letter++) {
                string driveLetter = letter + ":";
                StringBuilder s = new StringBuilder();
                if (QueryDosDevice(driveLetter, s, 1000)) {
                    drivePaths.Add(s.ToString(), driveLetter);
                }
            }

            // watch process creation/deletion events
            messageWindow = new MessageWindow();
            IsStarted = true;

            // watch active foreground window changes 
            dele = WhenActiveForegroundChanges;
            winActiveHook = SetWinEventHook(3, 3, IntPtr.Zero, dele, 0, 0, 0);
        }

        public static void Stop() {
            if (messageWindow != null) {
                messageWindow.Close();
                messageWindow.Dispose();
            }
            UnhookWinEvent(winActiveHook);
            dele = null;
        }

        public static void WindowCreation(IntPtr hwnd) {
            GetWindowThreadProcessId(hwnd, out uint processId);
            GetExecutablePathFromWindowHandle(hwnd, out string executablePath);

            if (executablePath != null) {
                if (executablePath.ToString().ToLower().StartsWith(@"c:\windows\")) {   // if this program is starting from here,
                    return;                                                             // we can assume it is not a game
                }
            }
            if (processId != 0 && AutoDetectGame((int)processId, executablePath, hwnd)) {
                Logger.WriteLine($"WindowCreation -- {processId} {hwnd} {executablePath}");
            }
        }

        public static void WindowDeletion(IntPtr hwnd) {
            GetWindowThreadProcessId(hwnd, out uint processId);

            if (processId != 0 && RecordingService.GetCurrentSession().Pid == processId && RecordingService.GetCurrentSession().WindowHandle == hwnd) {
                GetExecutablePathFromWindowHandle(hwnd, out string executablePath);
                Logger.WriteLine($"WindowDeletion -- {processId} {hwnd} {executablePath}");
                RecordingService.StopRecording();
            }
        }

        public static void CheckAlreadyRunningPrograms() {
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
        }

        static void WhenActiveForegroundChanges(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            GetForegroundProcess(out int pid, out _);
            if (RecordingService.IsRecording) {
                if (pid == RecordingService.GetCurrentSession().Pid) RecordingService.GainedFocus();
                else if (RecordingService.GameInFocus) RecordingService.LostFocus();
                return;
            }
        }

        public static void GetExecutablePathFromWindowHandle(nint hwnd, out string executablePath) {
            GetWindowThreadProcessId(hwnd, out uint processId);
            Process process = Process.GetProcessById((int)processId);

            try {
                // if this raises an exception, then that means this process is most likely being covered by anti-cheat (EAC)
                executablePath = Path.GetFullPath(process.MainModule.FileName);
            }
            catch (Exception ex) {
                // this method of using OpenProcess is reliable for getting the fullpath in case of anti-cheat
                IntPtr processHandle = OpenProcess(0x1000, false, process.Id);
                if (processHandle != IntPtr.Zero) {
                    StringBuilder stringBuilder = new(1024);
                    if (!GetProcessImageFileName(processHandle, stringBuilder, out int size)) {
                        Logger.WriteLine($"Failed to get process: [{processId}] full path. Error: {ex.Message}");
                        executablePath = "";
                        return;
                    }
                    string s = stringBuilder.ToString();
                    foreach (var drivePath in drivePaths) {
                        if (s.Contains(drivePath.Key)) s = s.Replace(drivePath.Key, drivePath.Value);
                    }

                    executablePath = s;
                    CloseHandle(processHandle);
                }
                else {
                    Logger.WriteLine($"Failed to get process: [{processId}][{process.ProcessName}] full path. Error: {ex.Message}");
                    executablePath = "";
                    return;
                }
            }
        }

        public static void LoadDetections() {
            gameDetectionsJson = DownloadDetections(gameDetectionsFile, "game_detections.json");
            nonGameDetectionsJson = DownloadDetections(nonGameDetectionsFile, "nongame_detections.json");
            LoadNonGameCache();
        }

        public static JsonElement[] DownloadDetections(string dlPath, string file) {
            var result = string.Empty;
            try {
                using (var webClient = new System.Net.WebClient()) {
                    result = webClient.DownloadString("https://raw.githubusercontent.com/lulzsun/RePlaysTV/master/detections/" + file);
                }
                File.WriteAllText(dlPath, result);
            }
            catch (Exception e) {
                Logger.WriteLine(e.Message);

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
            if (IsMatchedNonGame(executablePath)) return false;

            // If the windowHandle we captured is problematic, just return nothing
            // Problematic handles are created if the application for example,
            // the game displays a splash screen (SplashScreenClass) before launching
            // This detection is very primative and only covers specific cases, in the future we should find another way
            // to approach this issue. (possibily fetch to see if the window size ratio is not standard?)
            if (windowHandle == 0) windowHandle = RecordingService.ActiveRecorder.GetWindowHandleByProcessId(processId, true);
            var className = RecordingService.ActiveRecorder.GetClassName(windowHandle);
            string gameTitle = GetGameTitle(executablePath);
            string fileName = Path.GetFileName(executablePath);
            try {
                if (!Path.Exists(executablePath)) return false;
                FileVersionInfo fileInformation = FileVersionInfo.GetVersionInfo(executablePath);
                bool hasBadWordInDescription = fileInformation.FileDescription != null ? classBlacklist.Where(bannedWord => fileInformation.FileDescription.ToLower().Contains(bannedWord)).Any() : false;
                bool hasBadWordInClassName = classBlacklist.Where(bannedWord => className.ToLower().Contains(bannedWord)).Any() || classBlacklist.Where(bannedWord => className.ToLower().Replace(" ", "").Contains(bannedWord)).Any();
                bool hasBadWordInGameTitle = classBlacklist.Where(bannedWord => gameTitle.ToLower().Contains(bannedWord)).Any() || classBlacklist.Where(bannedWord => gameTitle.ToLower().Replace(" ", "").Contains(bannedWord)).Any();
                bool hasBadWordInFileName = classBlacklist.Where(bannedWord => fileName.ToLower().Contains(bannedWord)).Any() || classBlacklist.Where(bannedWord => fileName.ToLower().Replace(" ", "").Contains(bannedWord)).Any();

                bool isBlocked = hasBadWordInDescription || hasBadWordInClassName || hasBadWordInGameTitle || hasBadWordInFileName;
                if (isBlocked) {
                    Logger.WriteLine($"Blocked application: {windowHandle} {className} {gameTitle} {executablePath}");
                    return false;
                }
            }
            catch (Exception e) {
                Logger.WriteLine($"Failed to check blacklist for application: {executablePath} with error message: {e.Message}");
            }

            if (!autoRecord) {
                // This is a manual record event so lets just yolo it and assume user knows best
                RecordingService.SetCurrentSession(processId, windowHandle, gameTitle, executablePath);
                Logger.WriteLine($"Manual record start: [{processId}][{executablePath}], prepared to record");
                return true;
            }

            bool isGame = IsMatchedGame(executablePath);

            if (isGame) {
                RecordingService.SetCurrentSession(processId, windowHandle, gameTitle, executablePath);
                Logger.WriteLine(
                    $"This process is a recordable game: [{processId}][{executablePath}], prepared to record");

                bool allowed = SettingsService.Settings.captureSettings.recordingMode is "automatic" or "whitelist";
                Logger.WriteLine("Is allowed to record: " + allowed);
                if (allowed) RecordingService.StartRecording();
            }
            else {
                var windowSize = BaseRecorder.GetWindowSize(windowHandle);
                if (windowSize.GetWidth() <= 69 || windowSize.GetHeight() <= 69) {
                    return false;
                }
                Logger.WriteLine($"Unknown application: [{processId}]" +
                    $"[{className}]" +
                    $"[{windowSize.GetWidth()}x{windowSize.GetHeight()}]" +
                    $"[{executablePath}]");
            }
            return isGame;
        }

        public static bool HasBadWordInClassName(IntPtr windowHandle) {
            var className = RecordingService.ActiveRecorder.GetClassName(windowHandle);
            bool hasBadWordInClassName = classBlacklist.Any(bannedWord => className.ToLower().Contains(bannedWord)) || classBlacklist.Any(bannedWord => className.ToLower().Replace(" ", "").Contains(bannedWord));
            if (hasBadWordInClassName) windowHandle = IntPtr.Zero;
            return windowHandle == IntPtr.Zero;
        }

        public static bool IsMatchedGame(string exeFile) {
            exeFile = exeFile.ToLower();
            foreach (var game in SettingsService.Settings.detectionSettings.whitelist) {
                if (game.gameExe == exeFile) return true;
            }
            if (SettingsService.Settings.captureSettings.recordingMode == "whitelist") return false;

            for (int x = 0; x < gameDetectionsJson.Length; x++) {
                JsonElement[] gameDetections = gameDetectionsJson[x].GetProperty("mapped").GetProperty("game_detection").EnumerateArray().ToArray();

                for (int y = 0; y < gameDetections.Length; y++) {
                    bool d1 = gameDetections[y].TryGetProperty("gameexe", out JsonElement detection1);
                    bool d2 = gameDetections[y].TryGetProperty("launchexe", out JsonElement detection2);
                    string[] jsonExeStr = Array.Empty<string>();

                    if (d1) {
                        jsonExeStr = detection1.GetString().ToLower().Split('|');
                    }

                    if (!d1 && d2) {
                        jsonExeStr = detection2.GetString().ToLower().Split('|');
                    }

                    if (jsonExeStr.Length > 0) {
                        for (int z = 0; z < jsonExeStr.Length; z++) {
                            // TODO: use proper regex to check fullpaths instead of just filenames
                            if (Path.GetFileName(jsonExeStr[z]).Equals(Path.GetFileName(exeFile.ToLower())) && jsonExeStr[z].Length > 0) {
                                return true;
                            }
                        }
                    }
                }
            }

            // TODO: also parse Epic games/Origin games
            if (exeFile.Replace("\\", "/").Contains("/steamapps/common/"))
                return true;
            return false;
        }

        public static string GetGameTitle(string exeFile) {
            foreach (var game in SettingsService.Settings.detectionSettings.whitelist) {
                if (game.gameExe == exeFile.ToLower()) return game.gameName;
            }

            for (int x = 0; x < gameDetectionsJson.Length; x++) {
                JsonElement[] gameDetections = gameDetectionsJson[x].GetProperty("mapped").GetProperty("game_detection").EnumerateArray().ToArray();

                for (int y = 0; y < gameDetections.Length; y++) {
                    bool d1 = gameDetections[y].TryGetProperty("gameexe", out JsonElement detection1);
                    bool d2 = gameDetections[y].TryGetProperty("launchexe", out JsonElement detection2);
                    string[] jsonExeStr = Array.Empty<string>();

                    if (d1) {
                        jsonExeStr = detection1.GetString().ToLower().Split('|');
                    }

                    if (!d1 && d2) {
                        jsonExeStr = detection2.GetString().ToLower().Split('|');
                    }

                    if (jsonExeStr.Length > 0) {
                        for (int z = 0; z < jsonExeStr.Length; z++) {
                            // TODO: use proper regex to check fullpaths instead of just filenames
                            if (Path.GetFileName(jsonExeStr[z]).Equals(Path.GetFileName(exeFile.ToLower())) && jsonExeStr[z].Length > 0) {
                                return gameDetectionsJson[x].GetProperty("title").ToString();
                            }
                        }
                    }
                }
            }
            // Check to see if path is a steam game, and parse name
            // TODO: also parse Epic games/Origin games
            if (exeFile.ToLower().Replace("\\", "/").Contains("/steamapps/common/"))
                return Regex.Split(exeFile.Replace("\\", "/"), "/steamapps/common/", RegexOptions.IgnoreCase)[1].Split('/')[0];
            return "Game Unknown";
        }

        public static bool IsMatchedNonGame(string executablePath) {
            if (executablePath is null) return false;

            executablePath = executablePath.ToLower();

            if (SettingsService.Settings.detectionSettings.blacklist.Contains(executablePath)) {
                return true;
            }

            if (nonGameDetectionsCache.Contains(Path.GetFileName(executablePath))) {
                return true;
            }

            return false;
        }

        private static void LoadNonGameCache() {
            nonGameDetectionsCache.Clear();
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

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, Int32 dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        static extern bool GetProcessImageFileName(IntPtr hprocess, StringBuilder lpExeName, out int size);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);
    }

    // https://stackoverflow.com/a/7033382
    class MessageWindow : Form {
        private readonly int msgNotify;
        public delegate void WindowHandleEvent(object sender, object data);

        public MessageWindow() {
            var accessHandle = this.Handle;

            // Hook on to the shell for window creation/delection events
            msgNotify = RegisterWindowMessage("SHELLHOOK");
            _ = RegisterShellHookWindow(this.Handle);
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ChangeToMessageOnlyWindow();
        }

        private void ChangeToMessageOnlyWindow() {
            IntPtr HWND_MESSAGE = new(-3);
            SetParent(this.Handle, HWND_MESSAGE);
        }

        protected override void WndProc(ref Message m) {
            if (DetectionService.IsStarted && m.Msg == msgNotify) {
                // Receive shell messages
                switch (m.WParam.ToInt32()) {
                    case 1:  // HSHELL_WINDOWCREATED
                    case 4:  // HSHELL_WINDOWACTIVATED
                    case 13: // HSHELL_WINDOWREPLACED 
                        DetectionService.WindowCreation(m.LParam);
                        break;
                    case 2: // HSHELL_WINDOWDESTROYED
                        DetectionService.WindowDeletion(m.LParam);
                        break;
                }
            }
            base.WndProc(ref m);
        }

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int RegisterShellHookWindow(IntPtr hWnd);

        [DllImport("user32", EntryPoint = "RegisterWindowMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int RegisterWindowMessage(string lpString);
    }
}
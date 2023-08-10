using RePlays.Recorders;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static RePlays.Utils.Functions;

namespace RePlays.Services {
    public static class DetectionService {
        static readonly ManagementEventWatcher pCreationWatcher = new(new EventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));
        static readonly ManagementEventWatcher pDeletionWatcher = new(new EventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));

        static MessageWindow messageWindow;
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        static WinEventProc winActiveDele, winResizeDele; // Keep win event delegates alive as long as class is alive (if you dont do this, gc will clean up)
        static nint winActiveHook, winResizeHook;

        static JsonElement[] gameDetectionsJson;
        static JsonElement[] nonGameDetectionsJson;
        static readonly HashSet<string> nonGameDetectionsCache = new();
        static readonly string gameDetectionsFile = Path.Join(GetCfgFolder(), "gameDetections.json");
        static readonly string nonGameDetectionsFile = Path.Join(GetCfgFolder(), "nonGameDetections.json");
        private static Dictionary<string, string> drivePaths = new();
        private static List<string> classBlacklist = new() { "splashscreen", "launcher", "cheat", "sdl_app", "console" };
        private static List<string> classWhitelist = new() { "unitywndclass", "unrealwindow", "riotwindowclass" };
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
            //pCreationWatcher.EventArrived += ...;
            //pCreationWatcher.Start();
            pDeletionWatcher.EventArrived += (object sender, EventArrivedEventArgs e) => {
                try {
                    if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription) {
                        uint processId = uint.Parse(instanceDescription.GetPropertyValue("Handle").ToString());
                        //var executablePath = instanceDescription.GetPropertyValue("ExecutablePath");
                        //var cmdLine = instanceDescription.GetPropertyValue("CommandLine");

                        WindowDeletion(0, processId);
                    }
                }
                catch (ManagementException) { }
            };
            pDeletionWatcher.Start();

            // watch window creation/deletion events
            messageWindow = new MessageWindow();
            IsStarted = true;

            // watch active foreground window events 
            winActiveDele = OnActiveForegroundEvent;
            winActiveHook = SetWinEventHook(3, 3, IntPtr.Zero, winActiveDele, 0, 0, 0);

            // watch window resize/move events 
            winResizeDele = OnWindowResizeMoveEvent;
            winResizeHook = SetWinEventHook(11, 11, IntPtr.Zero, winResizeDele, 0, 0, 0);
        }

        public static void Stop() {
            if (messageWindow != null) {
                messageWindow.Close();
                messageWindow.Dispose();
            }
            pCreationWatcher.Stop();
            pDeletionWatcher.Stop();
            pCreationWatcher.Dispose();
            pDeletionWatcher.Dispose();
            UnhookWinEvent(winActiveHook);
            UnhookWinEvent(winResizeHook);
            winActiveDele = null;
            winResizeDele = null;
        }

        public static void WindowCreation(IntPtr hwnd, uint processId = 0, [CallerMemberName] string memberName = "") {
            if (processId == 0 && hwnd != 0)
                GetWindowThreadProcessId(hwnd, out processId);
            else if (processId == 0 && hwnd == 0)
                return;

            GetExecutablePathFromProcessId(processId, out string executablePath);

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
                GetWindowThreadProcessId(hwnd, out processId);
            else if (processId == 0 && hwnd == 0)
                return;

            GetExecutablePathFromProcessId(processId, out string executablePath);
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

        static void OnActiveForegroundEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            GetForegroundProcess(out int pid, out _);
            if (RecordingService.IsRecording) {
                if (pid == RecordingService.GetCurrentSession().Pid) RecordingService.GainedFocus();
                else if (RecordingService.GameInFocus) RecordingService.LostFocus();
                return;
            }
            else {
                WindowCreation(hwnd);
            }
        }

        static void OnWindowResizeMoveEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (RecordingService.IsRecording) {
                if (pid == RecordingService.GetCurrentSession().Pid && hwnd == RecordingService.GetCurrentSession().WindowHandle) {
                    var windowSize = BaseRecorder.GetWindowSize(hwnd);
                    Logger.WriteLine($"WindowResize: [{hwnd}][{windowSize.GetWidth()}x{windowSize.GetHeight()}]");
                }
                return;
            }
        }

        public static void GetExecutablePathFromProcessId(uint processId, out string executablePath) {
            if (processId == 0) {
                executablePath = "";
                return;
            }

            Process process;
            string processName = "Unknown";
            try {
                process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch {
                executablePath = "";
                return;
            }

            try {
                // if this raises an exception, then that means this process is most likely being covered by anti-cheat (EAC)
                executablePath = Path.GetFullPath(process.MainModule.FileName);
            }
            catch (Exception ex) {
                // this method of using OpenProcess is reliable for getting the fullpath in case of anti-cheat
                IntPtr processHandle = OpenProcess(0x00000400 | 0x00000010, false, (int)processId);
                if (processHandle != IntPtr.Zero) {
                    StringBuilder stringBuilder = new(1024);
                    if (!GetProcessImageFileName(processHandle, stringBuilder, out int _)) {
                        Logger.WriteLine($"Failed to get process: [{processId}] full path. Error: {ex.Message}");
                        executablePath = "";
                    }
                    else {
                        string s = stringBuilder.ToString();
                        foreach (var drivePath in drivePaths) {
                            if (s.Contains(drivePath.Key)) s = s.Replace(drivePath.Key, drivePath.Value);
                        }
                        executablePath = s;
                    }
                    CloseHandle(processHandle);
                    return;
                }
                else {
                    Logger.WriteLine($"Failed to get process: [{processId}][{processName}] full path. Error: {ex.Message}");
                    executablePath = "";
                    return;
                }
            }
        }

        public static void LoadDetections() {
            gameDetectionsJson = DownloadDetections(gameDetectionsFile, "gameDetections.json");
            nonGameDetectionsJson = DownloadDetections(nonGameDetectionsFile, "nonGameDetections.json");
            LoadNonGameCache();
        }

        public static JsonElement[] DownloadDetections(string dlPath, string file) {
            var result = string.Empty;
            var hash = "";
            try {
                // check if current file sha matches remote or not, if it does, we are already up-to-date
                if (File.Exists(dlPath)) {
                    using var sha1 = SHA1.Create();
                    using var stream = File.OpenRead(dlPath);
                    byte[] contentBytes = Encoding.ASCII.GetBytes($"blob {stream.Length}\0");
                    sha1.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);

                    byte[] fileBytes = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = stream.Read(fileBytes, 0, fileBytes.Length)) > 0) {
                        sha1.TransformBlock(fileBytes, 0, bytesRead, fileBytes, 0);
                    }
                    sha1.TransformFinalBlock(fileBytes, 0, 0);
                    hash = BitConverter.ToString(sha1.Hash).Replace("-", "").ToLower();

                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "RePlays Client");
                    var getTask = httpClient.GetAsync("https://api.github.com/repos/lulzsun/RePlays/contents/Resources/" + file);
                    getTask.Wait();
                    if (hash != "" && getTask.Result.Headers.ETag != null && getTask.Result.Headers.ETag.ToString().Contains(hash)) {
                        return JsonDocument.Parse(File.ReadAllText(dlPath)).RootElement.EnumerateArray().ToArray();
                    }
                }
                // download detections
                using (var httpClient = new HttpClient()) {
                    var getTask = httpClient.GetStringAsync("https://raw.githubusercontent.com/lulzsun/RePlays/main/Resources/" + file);
                    getTask.Wait();
                    result = getTask.Result;
                }
                File.WriteAllText(dlPath, result);
                Logger.WriteLine($"Downloaded {file} sha1={hash}");
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
            if (processId == 0) {
                Logger.WriteLine($"Process id should never be zero here, developer error?");
                return false;
            }
            if (executablePath != "" && IsMatchedNonGame(executablePath)) {
                //Logger.WriteLine($"Blacklisted application: [{processId}][{executablePath}]");
                return false;
            }

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

            bool isGame = IsMatchedGame(executablePath);
            var windowSize = BaseRecorder.GetWindowSize(windowHandle);
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
                RecordingService.SetCurrentSession(processId, windowHandle, gameTitle, executablePath);
                if (allowed) RecordingService.StartRecording();
            }
            return isGame;
        }

        public static bool HasBadWordInClassName(IntPtr windowHandle) {
            var className = RecordingService.ActiveRecorder.GetClassName(windowHandle);
            bool hasBadWordInClassName = classBlacklist.Any(c => className.ToLower().Contains(c)) || classBlacklist.Any(c => className.ToLower().Replace(" ", "").Contains(c));
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
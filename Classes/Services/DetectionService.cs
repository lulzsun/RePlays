using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RePlays.Utils;
using static RePlays.Utils.Functions;
using static RePlays.Services.RecordingService;

namespace RePlays.Services {
    public static class DetectionService {
        static readonly ManagementEventWatcher pCreationWatcher = new(new EventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));
        static readonly ManagementEventWatcher pDeletionWatcher = new(new EventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        // Keep delegate alive as long as class is alive.
        static WinEventDelegate dele;
        static IntPtr winActiveHook = IntPtr.Zero;

        static JsonElement[] gameDetectionsJson;
        static JsonElement[] nonGameDetectionsJson;
        static readonly string gameDetectionsFile = Path.Join(GetCfgFolder(), "gameDetections.json");
        static readonly string nonGameDetectionsFile = Path.Join(GetCfgFolder(), "nonGameDetections.json");
        private static Dictionary<string, string> drivePaths = new();
        private static List<string> blacklistList = new() { "splashscreen", "launcher", "cheat", "sdl_app" };

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
            pCreationWatcher.EventArrived += ProcessCreation_EventArrived;
            pDeletionWatcher.EventArrived += ProcessDeletion_EventArrived;
            pCreationWatcher.Start();
            pDeletionWatcher.Start();

            // watch active foreground window changes 
            dele = WhenActiveForegroundChanges;
            winActiveHook = SetWinEventHook(3, 3, IntPtr.Zero, dele, 0, 0, 0);
        }
        public static void Stop() {
            pCreationWatcher.Stop();
            pDeletionWatcher.Stop();
            pCreationWatcher.Dispose();
            pDeletionWatcher.Dispose();

            UnhookWinEvent(winActiveHook);
            dele = null;
        }

        static void ProcessCreation_EventArrived(object sender, EventArrivedEventArgs e) {
            if (RecordingService.IsRecording) return;

            try {
                if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription) {
                    int processId = int.Parse(instanceDescription.GetPropertyValue("Handle").ToString());
                    var executablePath = instanceDescription.GetPropertyValue("ExecutablePath");
                    //var cmdLine = instanceDescription.GetPropertyValue("CommandLine"); may or may not be useful in the future

                    if (executablePath != null) {
                        if (executablePath.ToString().ToLower().StartsWith(@"c:\windows\")) {   // if this program is starting from here,
                            return;                                                             // we can assume it is not a game
                        }
                    }
                    if (processId != 0) AutoDetectGame(processId);
                }
            }
            catch (ManagementException) { }

            e.NewEvent.Dispose();
        }

        public static void CheckAlreadyRunningPrograms() {
            Process[] processCollection = Array.Empty<Process>();

            try {
                processCollection = Process.GetProcesses()
                .Where(p => (long)p.MainWindowHandle != 0)
                .ToArray();
            }
            catch (Exception ex) {
                Logger.WriteLine($"Error: {ex.Message}");
            }
                
            foreach (Process process in processCollection) {             
                if (RecordingService.IsRecording) return;

                try {
                    if (!process.HasExited) AutoDetectGame(process.Id);
                }
                catch (Exception ex) {
                    Logger.WriteLine(ex.Message);
                }
            }
        }

        static void ProcessDeletion_EventArrived(object sender, EventArrivedEventArgs e) {
            if (!RecordingService.IsRecording) return;

            try {
                if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription) {
                    int processId = int.Parse(instanceDescription.GetPropertyValue("Handle").ToString());

                    if (processId != 0) {
                        if (RecordingService.GetCurrentSession().Pid == processId)
                            RecordingService.StopRecording();
                    }
                }
            }
            catch (ManagementException) { }

            e.NewEvent.Dispose();
        }

        static void WhenActiveForegroundChanges(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            int pid = GetForegroundProcessId();
            if (RecordingService.IsRecording) {
                if (pid == RecordingService.GetCurrentSession().Pid) RecordingService.GainedFocus();
                else if (RecordingService.GameInFocus) RecordingService.LostFocus();
                return;
            }

            //Don't really know why we were doing this, it makes not recording a game impossible
            //AutoDetectGame(GetForegroundProcessId());
        }

        public static void LoadDetections() {
            gameDetectionsJson = DownloadDetections(gameDetectionsFile, "game_detections.json");
            nonGameDetectionsJson = DownloadDetections(nonGameDetectionsFile, "nongame_detections.json");
        }
        public static void DisposeDetections() {
            gameDetectionsJson = null;
            nonGameDetectionsJson = null;
        }

        public static JsonElement[] DownloadDetections(string dlPath, string file) {
            var result = string.Empty;
            try {
                using (var webClient = new System.Net.WebClient()) {
                    result = webClient.DownloadString("https://raw.githubusercontent.com/lulzsun/RePlaysTV/master/detections/" + file);
                }
                File.WriteAllText(dlPath, result);
            }
            catch (System.Exception e) {
                Logger.WriteLine(e.Message);

                if(File.Exists(dlPath)) {
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
        public static async void AutoDetectGame(int processId, bool autoRecord = true) {
            Process process;
            string executablePath;
            string processName = "";
            try {
                process = Process.GetProcessById(processId);
                processName = process.ProcessName;
            }
            catch (Exception ex) {
                Logger.WriteLine($"Failed to open process: [{processId}]. Error: {ex.Message}");
                return;
            }
            try {
                executablePath = process.MainModule.FileName;
            }
            catch (Exception ex) {
                IntPtr processHandle = OpenProcess(0x1000, false, process.Id);
                if (processHandle != IntPtr.Zero) {
                    StringBuilder stringBuilder = new(1024);
                    if (!GetProcessImageFileName(processHandle, stringBuilder, out int size)) {
                        Logger.WriteLine($"Failed to get process: [{processId}] full path. Error: {ex.Message}");
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
                    Logger.WriteLine($"Failed to get process: [{processId}][{processName}] full path. Error: {ex.Message}");
                    return;
                }
            }

            // If the windowHandle we captured is problematic, just return nothing
            // Problematic handles are created if the application for example,
            // the game displays a splash screen (SplashScreenClass) before launching
            // This detection is very primative and only covers specific cases, in the future we should find another way
            // to approach this issue. (possibily fetch to see if the window size ratio is not standard?)
            var windowHandle = ActiveRecorder.GetWindowHandleByProcessId(processId, true);
            var className = ActiveRecorder.GetClassName(windowHandle);
            string gameTitle = GetGameTitle(executablePath);
            string fileName = Path.GetFileName(executablePath);
            try {
                FileVersionInfo fileInformation = FileVersionInfo.GetVersionInfo(executablePath);
                bool hasBadWordInDescription = fileInformation.FileDescription != null ? blacklistList.Where(bannedWord => fileInformation.FileDescription.ToLower().Contains(bannedWord)).Any() : false;
                bool hasBadWordInClassName = blacklistList.Where(bannedWord => className.ToLower().Contains(bannedWord)).Any() || blacklistList.Where(bannedWord => className.ToLower().Replace(" ", "").Contains(bannedWord)).Any();
                bool hasBadWordInGameTitle = blacklistList.Where(bannedWord => gameTitle.ToLower().Contains(bannedWord)).Any() || blacklistList.Where(bannedWord => gameTitle.ToLower().Replace(" ", "").Contains(bannedWord)).Any();
                bool hasBadWordInFileName = blacklistList.Where(bannedWord => fileName.ToLower().Contains(bannedWord)).Any() || blacklistList.Where(bannedWord => fileName.ToLower().Replace(" ", "").Contains(bannedWord)).Any();

                bool isBlocked = hasBadWordInDescription || hasBadWordInClassName || hasBadWordInGameTitle || hasBadWordInFileName;
                if (isBlocked) return;
                
            }
            catch(Exception e) {
                Logger.WriteLine($"Failed to check blacklist for application: {executablePath} with error message: {e.Message}");
            }

            if (IsMatchedNonGame(executablePath)) return;

            if (!autoRecord) {
                // This is a manual record event so lets just yolo it and assume user knows best
                RecordingService.SetCurrentSession(processId, gameTitle, executablePath);
                return;
            }

            bool isGame = IsMatchedGame(executablePath);

            if (!isGame && !executablePath.Contains(@":\Windows")) {
                Logger.WriteLine($"Process [{processId}][{Path.GetFileName(executablePath)}] isn't in the game detection list, checking if it might be a game");
                try {
                    var usage = GetGPUUsage(process.Id);
                    Logger.WriteLine($"PROCESS GPU USAGE [{process.Id}]: {usage}");
                    if (usage > 10) {
                        Logger.WriteLine(
                            $"This process [{processId}][{Path.GetFileName(executablePath)}], appears to be a game.");
                        isGame = true;
                    }
                }
                catch (Exception e) {
                    Logger.WriteLine(
                        $"Failed to evaluate gpu usage for [{Path.GetFileName(executablePath)}] isGame: {isGame}, reason: {e.Message}");
                }
            }

            if (isGame) {
                int tries = 0;
                while (tries < 40) {
                    process.Refresh();
                    if (process.MainWindowHandle == IntPtr.Zero) {
                        Logger.WriteLine($"Process [{processId}][{Path.GetFileName(executablePath)}]: Got no MainWindow. Retrying... {tries}/40");
                        await Task.Delay(1000);
                    }
                    else {
                        Logger.WriteLine($"Process [{processId}][{Path.GetFileName(executablePath)}]: Got MainWindow [{process.MainWindowTitle}]");
                        break;
                    }
                    tries++;
                }

                if (process.MainWindowHandle == IntPtr.Zero) return;

                RecordingService.SetCurrentSession(processId, gameTitle, executablePath);
                Logger.WriteLine(
                    $"This process [{processId}] is a recordable game [{Path.GetFileName(executablePath)}], prepared to record");

                bool allowed = SettingsService.Settings.captureSettings.recordingMode is "automatic" or "whitelist";
                Logger.WriteLine("Is allowed to record: " + allowed);
                if (allowed) RecordingService.StartRecording();
            }
            process.Dispose();
        }

        public static bool HasBadWordInClassName(IntPtr windowHandle) {
            var className = ActiveRecorder.GetClassName(windowHandle);
            bool hasBadWordInClassName = blacklistList.Any(bannedWord => className.ToLower().Contains(bannedWord)) || blacklistList.Any(bannedWord => className.ToLower().Replace(" ", "").Contains(bannedWord));
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
            if(exeFile.ToLower().Replace("\\", "/").Contains("/steamapps/common/"))
                return Regex.Split(exeFile.Replace("\\", "/"), "/steamapps/common/", RegexOptions.IgnoreCase)[1].Split('/')[0];
            return "Game Unknown";
        }

        public static bool IsMatchedNonGame(string exeFile) {
            if (exeFile == null)
                return false;

            exeFile = exeFile.ToLower();

            if (SettingsService.Settings.detectionSettings.blacklist.Contains(exeFile)) {
                return true;
            }
            for (int x = 0; x < nonGameDetectionsJson.Length; x++) {
                JsonElement[] gameDetections = nonGameDetectionsJson[x].GetProperty("detections").EnumerateArray().ToArray();

                for (int y = 0; y < gameDetections.Length; y++) {

                    if (gameDetections[y].TryGetProperty("detect_exe", out JsonElement detection)) {
                        string[] jsonExeStr = detection.GetString().ToLower().Split('|');
                        
                        for (int z = 0; z < jsonExeStr.Length; z++) {
                            // TODO: use proper regex to check fullpaths instead of just filenames
                            if (Path.GetFileName(jsonExeStr[z]).Equals(Path.GetFileName(exeFile)) && jsonExeStr[z].Length > 0)
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, Int32 dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern bool GetProcessImageFileName(IntPtr hprocess,
            StringBuilder lpExeName, out int size);
    }
}
﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using RePlays.Utils;
using static RePlays.Utils.Functions;

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
        public static void Start() {
            LoadDetections();
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

            try
            {
                if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription)
                {
                    int processId = int.Parse(instanceDescription.GetPropertyValue("Handle").ToString());
                    var executablePath = instanceDescription.GetPropertyValue("ExecutablePath");
                    //var cmdLine = instanceDescription.GetPropertyValue("CommandLine"); may or may not be useful in the future

                    if (executablePath != null)
                    {
                        if (executablePath.ToString().ToLower().StartsWith(@"c:\windows\"))
                        {   // if this program is starting from here,
                            return;                                                             // we can assume it is not a game
                        }
                    }
                    if (processId != 0) AutoDetectGame(processId);
                }
            }
            catch (ManagementException) { }

            e.NewEvent.Dispose();
        }
        static void ProcessDeletion_EventArrived(object sender, EventArrivedEventArgs e) {
            if (!RecordingService.IsRecording) return;

            try
            {
                if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription)
                {
                    int processId = int.Parse(instanceDescription.GetPropertyValue("Handle").ToString());

                    if (processId != 0)
                    {
                        if (RecordingService.GetCurrentSession().Pid == processId)
                            RecordingService.StopRecording();
                    }
                }
            }
            catch (ManagementException) { }

            e.NewEvent.Dispose();
        }
        static void WhenActiveForegroundChanges(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            if (RecordingService.IsRecording) return;
            AutoDetectGame(GetForegroundProcessId());
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
        public static void AutoDetectGame(int processId, bool autoRecord = true)
        {
            Process process;
            string executablePath;
            try
            {
                process = Process.GetProcessById(processId);
                executablePath = process.MainModule.FileName;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"Failed to get process: [{processId}] full path. Error: [{ex.Message}]");
                return;
            }

            if (IsMatchedNonGame(executablePath)) return;
            string gameTitle = GetGameTitle(executablePath);
            if (!autoRecord)
            {
                // This is a manual record event so lets just yolo it and assume user knows best
                RecordingService.SetCurrentSession(processId, gameTitle, executablePath);
                return;
            }

            bool isGame = IsMatchedGame(executablePath);

            if (!isGame)
            {
                Logger.WriteLine($"Process [{processId}]:[{Path.GetFileName(executablePath)}] isn't in the game detection list, checking if it might be a game");
                try
                {
                    var usage = GetGPUUsage(process.Id);
                    Logger.WriteLine($"PROCESS GPU USAGE [{process.Id}]: {usage}");
                    if (usage > 10)
                    {
                        Logger.WriteLine(
                            $"This process [{processId}]:[{Path.GetFileName(executablePath)}], appears to be a game.");
                        isGame = true;
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine(
                        $"Failed to evaluate gpu usage for [{Path.GetFileName(executablePath)}] isGame: {isGame}, reason: {e.Message}");
                }
            }

            if (isGame)
            {
                process.Refresh();
                if (process.MainWindowHandle == IntPtr.Zero) return;

                RecordingService.SetCurrentSession(processId, gameTitle, executablePath);
                Logger.WriteLine(
                    $"This process [{processId}] is a recordable game [{Path.GetFileName(executablePath)}], prepared to record");

                Logger.WriteLine("Is allowed to record: " + (SettingsService.Settings.captureSettings.recordingMode == "automatic"));
                if (SettingsService.Settings.captureSettings.recordingMode == "automatic")
                    RecordingService.StartRecording();
            }
            process.Dispose();
        }

        public static bool IsMatchedGame(string exeFile) {
            exeFile = exeFile.ToLower();
            foreach (var game in SettingsService.Settings.detectionSettings.whitelist) {
                if (game.gameExe == exeFile) return true;
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

    }
}
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using RePlays.Utils;
using static RePlays.Utils.Functions;

namespace RePlays.Services {
    public static class DetectionService {
        static JsonElement[] gameDetectionsJson;
        static JsonElement[] nonGameDetectionsJson;
        private static readonly string gameDetectionsFile = Path.Join(GetCfgFolder(), "gameDetections.json");
        private static readonly string nonGameDetectionsFile = Path.Join(GetCfgFolder(), "nonGameDetections.json");

        public static void DisposeDetections() {
            gameDetectionsJson = null;
            nonGameDetectionsJson = null;
        }

        public static void LoadDetections() {
            gameDetectionsJson = DownloadDetections(gameDetectionsFile, "game_detections.json");
            nonGameDetectionsJson = DownloadDetections(nonGameDetectionsFile, "nongame_detections.json");
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

        public static bool IsMatchedGame(string exeFile) {
            exeFile = exeFile.ToLower();

            if (SettingsService.Settings.advancedSettings.whitelist.Contains(exeFile)) {
                return true;
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
                            if (Path.GetFileName(jsonExeStr[z]).Equals(exeFile.ToLower()) && jsonExeStr[z].Length > 0) {
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
                            if (Path.GetFileName(jsonExeStr[z]).Equals(exeFile.ToLower()) && jsonExeStr[z].Length > 0) {
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
            exeFile = exeFile.ToLower();

            if (SettingsService.Settings.advancedSettings.blacklist.Contains(exeFile)) {
                return true;
            }
            for (int x = 0; x < nonGameDetectionsJson.Length; x++) {
                JsonElement[] gameDetections = nonGameDetectionsJson[x].GetProperty("detections").EnumerateArray().ToArray();

                for (int y = 0; y < gameDetections.Length; y++) {

                    if (gameDetections[y].TryGetProperty("detect_exe", out JsonElement detection)) {
                        string[] jsonExeStr = detection.GetString().ToLower().Split('|');
                        
                        for (int z = 0; z < jsonExeStr.Length; z++) {
                            if (Path.GetFileName(jsonExeStr[z]).Equals(exeFile) && jsonExeStr[z].Length > 0)
                                return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
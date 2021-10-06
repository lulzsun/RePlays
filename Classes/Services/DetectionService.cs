using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using static RePlays.Helpers.Functions;

namespace RePlays.Services {
    public class DetectionService {
        JsonElement[] gameDetectionsJson;
        JsonElement[] nonGameDetectionsJson;
        private static string gameDetectionsFile = Path.Join(GetCfgFolder(), "gameDetections.json");
        private static string nonGameDetectionsFile = Path.Join(GetCfgFolder(), "nonGameDetections.json");

        public void DisposeDetections() {
            gameDetectionsJson = null;
            nonGameDetectionsJson = null;
        }

        public void LoadDetections() {
            gameDetectionsJson = DownloadDetections(gameDetectionsFile, "game_detections.json");
            nonGameDetectionsJson = DownloadDetections(nonGameDetectionsFile, "nongame_detections.json");
        }

        public JsonElement[] DownloadDetections(string dlPath, string file) {
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

        public bool IsMatchedGame(string exeFile) {
            for (int x = 0; x < gameDetectionsJson.Length; x++) {
                JsonElement[] gameDetections = gameDetectionsJson[x].GetProperty("mapped").GetProperty("game_detection").EnumerateArray().ToArray();

                for (int y = 0; y < gameDetections.Length; y++) {

                    if (gameDetections[y].TryGetProperty("gameexe", out JsonElement detection)) {
                        string[] jsonExeStr = detection.GetString().ToLower().Split('|');

                        for (int z = 0; z < jsonExeStr.Length; z++) {
                            if (exeFile.ToLower().Contains(jsonExeStr[z]) && jsonExeStr[z].Length > 0) {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public string GetGameTitle(string exeFile, bool isUnknown=false) {
            if(isUnknown == false) {
                for (int x = 0; x < gameDetectionsJson.Length; x++) {
                    JsonElement[] gameDetections = gameDetectionsJson[x].GetProperty("mapped").GetProperty("game_detection").EnumerateArray().ToArray();

                    for (int y = 0; y < gameDetections.Length; y++) {

                        if (gameDetections[y].TryGetProperty("gameexe", out JsonElement detection)) {
                            string[] jsonExeStr = detection.GetString().ToLower().Split('|');

                            for (int z = 0; z < jsonExeStr.Length; z++) {
                                if (exeFile.ToLower().Contains(jsonExeStr[z]) && jsonExeStr[z].Length > 0) {
                                    return gameDetectionsJson[x].GetProperty("title").ToString();
                                }
                            }
                        }
                    }
                }
                return "Game Unknown";
            }

            // if isUnknown (if not on detection.json), check to see if path is a steam game, and parse name
            if(exeFile.Contains("\\steamapps\\common\\"))
                return exeFile.Split("\\steamapps\\common\\")[1].Split('\\')[0];
            return "Game Unknown";
        }

        public bool IsMatchedNonGame(string exeFile) {
            for (int x = 0; x < nonGameDetectionsJson.Length; x++) {
                JsonElement[] gameDetections = nonGameDetectionsJson[x].GetProperty("detections").EnumerateArray().ToArray();

                for (int y = 0; y < gameDetections.Length; y++) {

                    if (gameDetections[y].TryGetProperty("detect_exe", out JsonElement detection)) {
                        string[] jsonExeStr = detection.GetString().ToLower().Split('|');

                        for (int z = 0; z < jsonExeStr.Length; z++) {
                            if (exeFile.ToLower().Contains(jsonExeStr[z]) && jsonExeStr[z].Length > 0)
                                return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}

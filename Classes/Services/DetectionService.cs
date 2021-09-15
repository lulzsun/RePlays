using System.Linq;
using System.Text.Json;

namespace RePlays.Services {
    public class DetectionService {
        JsonElement[] gameDetectionsJson;
        JsonElement[] nonGameDetectionsJson;

        public void DisposeDetections() {
            gameDetectionsJson = null;
            nonGameDetectionsJson = null;
        }

        public void LoadDetections() {
            DownloadGameDetections();
            DownloadNonGameDetections();
        }

        public void DownloadGameDetections() {
            var result = string.Empty;
            using (var webClient = new System.Net.WebClient()) {
                result = webClient.DownloadString("https://raw.githubusercontent.com/lulzsun/RePlaysTV/master/detections/game_detections.json");
            }
            gameDetectionsJson = JsonDocument.Parse(result).RootElement.EnumerateArray().ToArray();
        }

        public void DownloadNonGameDetections() {
            var result = string.Empty;
            using (var webClient = new System.Net.WebClient()) {
                result = webClient.DownloadString("https://raw.githubusercontent.com/lulzsun/RePlaysTV/master/detections/nongame_detections.json");
            }
            nonGameDetectionsJson = JsonDocument.Parse(result).RootElement.EnumerateArray().ToArray();
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

        public string GetGameTitle(string exeFile) {
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

using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using RePlays.JSONObjects;

namespace RePlays.Services {
    public static class SettingsService {
        private static SettingsJson _Settings = new();
        public static SettingsJson Settings { get { return _Settings; } }
        private static string settingsFile = Path.Join(Application.StartupPath, "userSettings.json");

        public class SettingsJson {
            private string _videoSaveDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Plays");
            public string videoSaveDir { get { return _videoSaveDir; } set { _videoSaveDir = value; } }

            private string _tempSaveDir = Path.Join(Path.GetTempPath(), "Plays");
            public string tempSaveDir { get { return _tempSaveDir; } set { _tempSaveDir = value; } }

            private string _recordingMode = "automatic";
            public string recordingMode { get { return _recordingMode; } set { _recordingMode = value; } }

            private GameDvrSettings _gameDvrSettings = new();
            public GameDvrSettings gameDvrSettings { get { return _gameDvrSettings; } set { _gameDvrSettings = value; } }
        }

        public static void LoadSettings() {
            if (File.Exists(settingsFile)) {
                _Settings = JsonSerializer.Deserialize<SettingsJson>(File.ReadAllText(settingsFile));
                Logger.WriteLine("Loaded userSettings.json");
            }
            else {
                Logger.WriteLine(string.Format("{0} did not exist, using default values", settingsFile));
            }
        }

        public static void SaveSettings(SettingsJson settings=null) {
            if (settings == null) settings = Settings;
            Logger.WriteLine("Saved userSettings.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(settings, options));
        }
    }
}

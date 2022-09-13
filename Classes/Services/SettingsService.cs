using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using RePlays.Utils;
using static RePlays.Utils.Functions;

namespace RePlays.Services {
    public static class SettingsService {
        private static SettingsJson _Settings = new();
        public static SettingsJson Settings { get { return _Settings; } }
        private static string settingsFile = Path.Join(GetCfgFolder(), "userSettings.json");

        public class SettingsJson {
            private GeneralSettings _generalSettings = new();
            public GeneralSettings generalSettings { get { return _generalSettings; } set { _generalSettings = value; } }

            private CaptureSettings _captureSettings = new();
            public CaptureSettings captureSettings { get { return _captureSettings; } set { _captureSettings = value; } }

            private AdvancedSettings _advancedSettings = new();
            public AdvancedSettings advancedSettings { get { return _advancedSettings; } set { _advancedSettings = value; } }

            private UploadSettings _uploadSettings = new();
            public UploadSettings uploadSettings { get { return _uploadSettings; } set { _uploadSettings = value; } }

            public string[][][] availableKeybindings = new string[][][] { 
                new string[][]{ new string[] { "StartStopRecording" }, new string[] { "Control", "F9" } },
                new string[][]{ new string[] { "CreateBookmark" }, new string[] { "F8" } }
            };
            private Dictionary<string, string[]> _keybindings = new Dictionary<string, string[]>() { 
                { "StartStopRecording", new string[] { "Control", "F9" } },
                { "CreateBookmark", new string[] { "F8" } }
            };
            public Dictionary<string, string[]> keybindings { get { return _keybindings; } set { _keybindings = value; } }

            private List<CustomGame> _customGames = new();
            public List<CustomGame> customGames { get { return _customGames; } set { _customGames = value; } }
        }

        public static void LoadSettings() {
            if (File.Exists(settingsFile)) {
                try {
                    _Settings = JsonSerializer.Deserialize<SettingsJson>(File.ReadAllText(settingsFile));
                    Logger.WriteLine("Loaded userSettings.json");
                    checkForMissingKeybindings();
                }
                catch (JsonException ex) {
                    Logger.WriteLine(ex.Message);
                    File.Delete(settingsFile);
                    LoadSettings();
                }
            }
            else {
                Logger.WriteLine(string.Format("{0} did not exist, using default values", settingsFile));
            }
        }

        public static void SaveSettings(WebMessage webMessage) {
            SettingsJson data = JsonSerializer.Deserialize<SettingsJson>(webMessage.data);
            data.uploadSettings.streamableSettings.password = EncryptString(data.uploadSettings.streamableSettings.password);
            SaveSettings(data);
        }

        public static void SaveSettings(SettingsJson settings=null) {
            if (settings == null) settings = Settings;
            _Settings = settings;
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(settings, options));
            Logger.WriteLine("Saved userSettings.json");
        }

        private static void checkForMissingKeybindings(SettingsJson settings = null)
        {
            Logger.WriteLine("Checking for missing keybinds...");
            if (settings == null) settings = Settings;
            _Settings = settings;

            foreach (var keybind in _Settings.availableKeybindings)
            {
                var key = keybind[0][0];
                string[] value = new string[] { keybind[1][0] };
                bool foundKey = _Settings.keybindings.ContainsKey(key);
                if (!foundKey)
                {
                    Logger.WriteLine("Adding keybind " + key + " with value " + value.ToString());
                    _Settings.keybindings.Add(key, value);
                }
            }
            SaveSettings(_Settings);
        }
    }
}

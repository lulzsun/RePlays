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

            private StorageSettings _storageSettings = new();
            public StorageSettings storageSettings { get { return _storageSettings; } set { _storageSettings = value; } }

            private UploadSettings _uploadSettings = new();
            public UploadSettings uploadSettings { get { return _uploadSettings; } set { _uploadSettings = value; } }

            private DetectionSettings _detectionSettings = new();
            public DetectionSettings detectionSettings { get { return _detectionSettings; } set { _detectionSettings = value; } }

            private Dictionary<string, string[]> _keybindings = new Dictionary<string, string[]>() {};
            public Dictionary<string, string[]> keybindings { get { return _keybindings; } set { _keybindings = value; } }
        }

        public static void LoadSettings() {
            if (File.Exists(settingsFile)) {
                try {
                    _Settings = JsonSerializer.Deserialize<SettingsJson>(File.ReadAllText(settingsFile));
                    Logger.WriteLine("Loaded userSettings.json");
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
    }
}

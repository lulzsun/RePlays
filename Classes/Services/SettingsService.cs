using RePlays.Recorders;
using RePlays.Utils;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using static RePlays.Utils.Functions;

namespace RePlays.Services {
    public static class SettingsService {
        private static Object thisLock = new Object();
        private static SettingsJson _Settings = new();
        public static SettingsJson Settings { get { return _Settings; } }
        private static string settingsFile = Path.Join(GetCfgFolder(), "userSettings.json");

        public static JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public class SettingsJson {
            private GeneralSettings _generalSettings = new();
            public GeneralSettings generalSettings { get { return _generalSettings; } set { _generalSettings = value; } }

            private CaptureSettings _captureSettings = new();
            public CaptureSettings captureSettings { get { return _captureSettings; } set { _captureSettings = value; } }
            private ClipSettings _clipSettings = new();
            public ClipSettings clipSettings { get { return _clipSettings; } set { _clipSettings = value; } }

            private StorageSettings _storageSettings = new();
            public StorageSettings storageSettings { get { return _storageSettings; } set { _storageSettings = value; } }

            private UploadSettings _uploadSettings = new();
            public UploadSettings uploadSettings { get { return _uploadSettings; } set { _uploadSettings = value; } }

            private DetectionSettings _detectionSettings = new();
            public DetectionSettings detectionSettings { get { return _detectionSettings; } set { _detectionSettings = value; } }

            private KeybindSettings _keybindSettings = new();
            public KeybindSettings keybindSettings { get { return _keybindSettings; } set { _keybindSettings = value; } }
        }

        public static void LoadSettings() {
            if (File.Exists(settingsFile)) {
                try {
                    var content = File.ReadAllText(settingsFile);

                    // Trim characters after the last closing bracket
                    // Sometimes a bunch of junk chars gets saved after serializing for unknown reason...
                    // Maybe because my settings json is located on a winbtrs drive and is corrupting it?
                    // Not sure, but this fixes it...
                    if (content.LastIndexOf('}') != -1) content = content[..(content.LastIndexOf('}') + 1)];

                    _Settings = JsonSerializer.Deserialize<SettingsJson>(content);
                    Logger.WriteLine("Loaded userSettings.json");
                }
                catch (JsonException ex) {
                    Logger.WriteLine(ex.Message);
                    File.Delete(settingsFile);
                    LoadSettings();
                }
            }
            else {
                Logger.WriteLine($"{settingsFile} did not exist, using default values");
            }
        }

        private static string MergeJsonStrings(string jsonA, string jsonB) {
            var nodeA = JsonNode.Parse(jsonA);
            var nodeB = JsonNode.Parse(jsonB);

            MergeJsonNodes(nodeA.AsObject(), nodeB.AsObject());

            return nodeA.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private static void MergeJsonNodes(JsonObject target, JsonObject source) {
            foreach (var property in source) {
                if (target.TryGetPropertyValue(property.Key, out var targetValue)) {
                    if (property.Value is JsonObject sourceObject && targetValue is JsonObject targetObject) {
                        // Both are objects, recurse
                        MergeJsonNodes(targetObject, sourceObject);
                    }
                    else {
                        // Overwrite target property with source property
                        target[property.Key] = property.Value.DeepClone();
                    }
                }
                else {
                    // Property doesn't exist in target, add it
                    target.Add(property.Key, property.Value.DeepClone());
                }
            }
        }

        public static void SaveSetting(WebMessage webMessage) {
            var unsanitizeJsonString = JsonSerializer.Deserialize<string>(webMessage.data);
            string mergedJson = MergeJsonStrings(JsonSerializer.Serialize(Settings), unsanitizeJsonString);
            SaveSettings(new WebMessage() { data = mergedJson });
        }

        public static void SaveSettings(WebMessage webMessage) {
            SettingsJson data = JsonSerializer.Deserialize<SettingsJson>(webMessage.data, jsonOptions);
            data.uploadSettings.streamableSettings.password =
                data.uploadSettings.streamableSettings.password != Settings.uploadSettings.streamableSettings.password
                ? EncryptString(data.uploadSettings.streamableSettings.password)
                : Settings.uploadSettings.streamableSettings.password;
            data.uploadSettings.rePlaysSettings.password =
                data.uploadSettings.rePlaysSettings.password != Settings.uploadSettings.rePlaysSettings.password
                ? EncryptString(data.uploadSettings.rePlaysSettings.password)
                : Settings.uploadSettings.rePlaysSettings.password;
            if (data.captureSettings.useReplayBuffer == true && data.captureSettings.fileFormat.isReplayBufferCompatible == false) {
                data.captureSettings.fileFormat = new FileFormat("mp4", "MP4 (.mp4)", true);
            }
            SaveSettings(data);
        }

        public static void SaveSettings(SettingsJson settings = null) {
            SettingsJson oldSettings = Settings;
            if (settings == null) settings = Settings;
            _Settings = settings;
            var options = new JsonSerializerOptions { WriteIndented = true };
            lock (thisLock) {
                File.WriteAllText(settingsFile, JsonSerializer.Serialize(settings, options));
            }
            Logger.WriteLine("Saved userSettings.json");
            if (oldSettings.captureSettings.encoder != Settings.captureSettings.encoder) {
                if (RecordingService.ActiveRecorder is LibObsRecorder recorder) {
                    recorder.GetAvailableRateControls();
                }
            }
        }

        public static void UpdateGpuManufacturer() {
            if (_Settings.generalSettings != null) {
                _Settings.generalSettings.device.gpuManufacturer = GetGpuManufacturer();
                SaveSettings();
            }
        }
    }
}
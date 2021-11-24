using RePlays.Utils;
using NHotkey.WindowsForms;
using System.Windows.Forms;
using System;

namespace RePlays.Services {
    public static class HotkeyService {
        public static string EditId = null;

        public static void Start() {
            foreach (var keybind in SettingsService.Settings.keybindings) {
                RegisterHotkey(keybind.Key, keybind.Value);
            }
        }

        public static void Stop() {
            foreach (var keybind in SettingsService.Settings.keybindings) {
                HotkeyManager.Current.Remove(keybind.Key);
            }
        }

        public static void RegisterHotkey(string name, string[] keys) {
            Keys keybind = Keys.None;

            for (int i = 0; i < keys.Length; i++) {
                Keys key = Keys.None;
                Enum.TryParse(keys[i], out key);

                if(i == 0) keybind = key;
                else keybind |= key;
            }

            HotkeyManager.Current.AddOrReplace(name, keybind, (s, e) => {
                switch (name) {
                    case "StartStopRecording": {
                            if (!RecordingService.IsRecording) {
                                Logger.WriteLine("Manual Start Recording");
                                RecordingService.StartRecording();
                            }
                            else {
                                Logger.WriteLine("Manual Stop Recording");
                                RecordingService.StopRecording();
                            }
                        }
                        break;
                    default:
                        Logger.WriteLine($"No hotkey event match for {name}");
                        break;
                }
                e.Handled = true;
            });
            Logger.WriteLine($"Registered Hotkey: {name} / [{string.Join(" | ", keys)}]");
        }
    }
}

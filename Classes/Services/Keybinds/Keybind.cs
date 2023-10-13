#if WINDOWS
using RePlays.Services;
using RePlays.Utils;

namespace RePlays.Classes.Services.Keybinds {
    public abstract class Keybind {
        public string Id { get; internal set; }
        internal string[] DefaultKeys { get; set; }
        public string[] Keys { get; internal set; }

        public abstract void Action();

        public virtual void SetKeybind(string[] Keys = null) {
            if (Keys == null) {
                bool exists = SettingsService.Settings.keybindSettings.TryGetValue(Id, out CustomKeybind existingBind);
                if (!exists) {
                    SettingsService.Settings.keybindSettings[Id] = new CustomKeybind {
                        keys = DefaultKeys,
                    };
                    SettingsService.SaveSettings();
                    Keys = DefaultKeys;
                    Logger.WriteLine($"Set new keybind: Action={Id}, Keys={Keys}");
                }
                else {
                    Keys = existingBind.keys;
                    Logger.WriteLine($"Set existing keybind: Action={Id}, Keys={string.Join(",", Keys)}");
                }
            }
            else {
                SettingsService.Settings.keybindSettings[Id] = new CustomKeybind {
                    keys = Keys,
                };
                SettingsService.SaveSettings();
                Logger.WriteLine($"Set new keybind: Action={Id}, Keys={string.Join(",", Keys)}");
                WebMessage.SendMessage(Functions.GetUserSettings());
            }
            this.Keys = Keys;
        }
    }
}
#endif
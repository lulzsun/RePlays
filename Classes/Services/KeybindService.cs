using RePlays.Classes.Services.Keybinds;
using RePlays.Utils;
using SharpHook;
using System.Collections.Generic;
using System.Linq;

namespace RePlays.Services {
    public static class KeybindService {
        private static readonly List<Keybind> keybinds = [];
        private static readonly HashSet<string> pressedKeys = [];
        private static readonly HashSet<string> cachePressedKeys = [];
        static TaskPoolGlobalHook keyboardHook;

        public static string EditId { get; internal set; }

        public static void Start() {
            Logger.WriteLine("Starting KeybindService...");

            //Create keybinds
            keybinds.Add(new BookmarkKeybind());
            keybinds.Add(new RecordingKeybind());

            //Create hook
            keyboardHook = new TaskPoolGlobalHook();
            keyboardHook.KeyPressed += OnKeyPressed;
            keyboardHook.KeyReleased += OnKeyReleased;
            keyboardHook.RunAsync();
        }

        private static void OnKeyPressed(object? sender, KeyboardHookEventArgs e) {
            string keyCode = e.RawEvent.Keyboard.KeyCode.ToString()[2..];
            pressedKeys.Add(keyCode);
            if (EditId == null) {
                foreach (Keybind h in keybinds) {
                    if (string.Join(",", pressedKeys.OrderBy(s => s.ToString())) == string.Join(",", h.Keys.OrderBy(s => s.ToString())) &&
                        !pressedKeys.SetEquals(cachePressedKeys) && !SettingsService.Settings.keybindSettings[h.Id].disabled) {
                        h.Action();
                        Logger.WriteLine($"Key: [{string.Join(",", h.Keys)}], Action: [{h.Id}]");
                    }
                }
            }
            else {
                if (!pressedKeys.SetEquals(cachePressedKeys)) {
                    Logger.WriteLine($"KeysDown: [{string.Join(",", pressedKeys)}]");
                }
            }
            cachePressedKeys.Add(keyCode);
        }

        private static void OnKeyReleased(object? sender, KeyboardHookEventArgs e) {
            string keyCode = e.RawEvent.Keyboard.KeyCode.ToString()[2..];
            if (EditId != null) {
                int hkIndex = keybinds.FindIndex(h => h.Id == EditId);
                if (hkIndex == -1) {
                    Logger.WriteLine($"Error, could not find keybind action: {EditId}");
                }
                else {
                    keybinds[hkIndex].SetKeybind(pressedKeys.Select(p => p.ToString()).ToArray());
                }
                Logger.WriteLine($"Exiting keybind edit mode.");
                EditId = null;
            }
            pressedKeys.Remove(keyCode);
            cachePressedKeys.Remove(keyCode);
        }

        public static void Stop() {
            Logger.WriteLine("Stopping KeybindService...");

            keybinds.Clear();
            keyboardHook.KeyPressed -= OnKeyPressed;
            keyboardHook.KeyReleased -= OnKeyReleased;
            keyboardHook.Dispose();
        }
    }
}
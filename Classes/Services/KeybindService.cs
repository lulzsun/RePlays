using RePlays.Classes.Services.Keybinds;
using RePlays.Utils;
using SharpHook;
using SharpHook.Data;
using SharpHook.Providers;
using System.Collections.Generic;
using System.Linq;

namespace RePlays.Services {
    public static class KeybindService {
        private static readonly List<Keybind> keybinds = [];
        private static readonly HashSet<string> pressedKeys = [];
        private static readonly HashSet<string> cachePressedKeys = [];
        static SimpleGlobalHook globalHook;

        public static string EditId { get; internal set; }

        public static void Start() {
            Logger.WriteLine("Starting KeybindService...");

            //Create keybinds
            keybinds.Add(new BookmarkKeybind());
            keybinds.Add(new RecordingKeybind());

            //Create hook
            UioHookProvider.Instance.KeyTypedEnabled = false;
            globalHook = new SimpleGlobalHook(UioHookProvider.Instance);
            globalHook.KeyPressed += OnKeyPressed;
            globalHook.KeyReleased += OnKeyReleased;
            globalHook.MousePressed += OnMousePressed;
            globalHook.MouseReleased += OnMouseReleased;
            globalHook.RunAsync(GlobalHookType.All, true);
        }

        private static void OnKeyPressed(object? sender, KeyboardHookEventArgs e) {
            OnInputPressed(GetKeyName(e.Data.KeyCode));
        }

        private static void OnKeyReleased(object? sender, KeyboardHookEventArgs e) {
            OnInputReleased(GetKeyName(e.Data.KeyCode));
        }

        private static void OnMousePressed(object? sender, MouseHookEventArgs e) {
            string name = GetMouseButtonName(e.Data.Button);
            if (name != null) OnInputPressed(name);
        }

        private static void OnMouseReleased(object? sender, MouseHookEventArgs e) {
            string name = GetMouseButtonName(e.Data.Button);
            if (name != null) OnInputReleased(name);
        }

        private static string GetKeyName(KeyCode keyCode) {
            // Strip the "Vc" prefix; this is the format stored in user settings.
            return keyCode.ToString()[2..];
        }

        private static string GetMouseButtonName(MouseButton button) {
            return button switch {
                MouseButton.Button2 => "MouseRight",
                MouseButton.Button3 => "MouseMiddle",
                MouseButton.Button4 => "Mouse4",
                MouseButton.Button5 => "Mouse5",
                _ => null,
            };
        }

        private static void OnInputPressed(string keyCode) {
            pressedKeys.Add(keyCode);
            if (EditId == null) {
                foreach (Keybind h in keybinds) {
                    if (pressedKeys.IsSupersetOf(h.Keys) &&
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

        private static void OnInputReleased(string keyCode) {
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
            globalHook.KeyPressed -= OnKeyPressed;
            globalHook.KeyReleased -= OnKeyReleased;
            globalHook.MousePressed -= OnMousePressed;
            globalHook.MouseReleased -= OnMouseReleased;
            globalHook.Dispose();
        }
    }
}

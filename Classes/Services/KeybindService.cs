using RePlays.Classes.Services.Keybinds;
using RePlays.Utils;
using SharpHook;
using System.Collections.Generic;

namespace RePlays.Services {
    public static class KeybindService {
        private static readonly List<Keybind> keybinds = new();
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
            keyboardHook.RunAsync();
        }

        private static void OnKeyPressed(object? sender, KeyboardHookEventArgs e) {
            Logger.WriteLine($"Pressed: {e.RawEvent.Keyboard.KeyCode} - {e.RawEvent.Keyboard.RawCode}");
        }

        public static void Stop() {
            Logger.WriteLine("Stopping KeybindService...");

            keybinds.Clear();
            keyboardHook.Dispose();
        }
    }
}
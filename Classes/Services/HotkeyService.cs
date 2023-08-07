#if WINDOWS
using RePlays.Classes.Services.Hotkeys;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RePlays.Services {
    public static class HotkeyService {
        public delegate IntPtr WinHookProc(int Code, IntPtr W, IntPtr L);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr SetWindowsHookEx(int idHook, WinHookProc lpfn, int hInstance, int threadId);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(IntPtr idHook);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        private static readonly List<Hotkey> hotkeys = new();
        private static readonly HashSet<Keys> pressedKeys = new();
        static nint keyboardHook;
        static WinHookProc keyboardDele;

        public static void Start() {
            //Create hotkeys
            hotkeys.Add(new BookmarkHotkey());
            hotkeys.Add(new RecordingHotkey());

            //Create hook
            keyboardDele = OnKeyEvent;
            keyboardHook = SetWindowsHookEx(13, keyboardDele, 0, 0);
            Logger.WriteLine("Loaded KeyboardHook...");
        }

        public static void Stop() {
            hotkeys.Clear();
            UnhookWindowsHookEx(keyboardHook);
            Logger.WriteLine("Unloaded KeyboardHook...");
        }

        private static IntPtr OnKeyEvent(int Code, IntPtr W, IntPtr L) {
            if (Code < 0)
                return CallNextHookEx(keyboardHook, Code, W, L);

            try {
                KeyEvents kEvent = (KeyEvents)W;
                Keys vkCode = (Keys)Marshal.ReadInt32(L);
                if (kEvent == KeyEvents.KeyDown) {
                    pressedKeys.Add(vkCode);
                    Logger.WriteLine($"Keys: [{string.Join(",", pressedKeys)}]");
                    foreach (Hotkey h in hotkeys) {
                        if (vkCode == h.Keybind) {
                            h.Action();
                            Logger.WriteLine($"Key: [{h.Keybind}], Action: [{h.GetType()}]");
                        }
                    }
                }
                else if (kEvent == KeyEvents.KeyUp) {
                    pressedKeys.Remove(vkCode);
                    Logger.WriteLine($"Keys: [{string.Join(",", pressedKeys)}]");
                }
            }
            catch (Exception e) {
                Logger.WriteLine("Error getting current keypress: " + e.ToString());
            }

            return CallNextHookEx(keyboardHook, Code, W, L);
        }

        public enum KeyEvents {
            KeyDown = 0x0100,
            KeyUp = 0x0101
        }
    }
}
#endif
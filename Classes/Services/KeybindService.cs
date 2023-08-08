#if WINDOWS
using RePlays.Classes.Services.Keybinds;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RePlays.Services {
    public static class KeybindService {
        public delegate IntPtr WinHookProc(int Code, IntPtr W, IntPtr L);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr SetWindowsHookEx(int idHook, WinHookProc lpfn, int hInstance, int threadId);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(IntPtr idHook);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        private static readonly List<Keybind> keybinds = new();
        private static readonly HashSet<Keys> pressedKeys = new();
        private static readonly HashSet<Keys> cachePressedKeys = new();
        static nint keyboardHook;
        static WinHookProc keyboardDele;

        public static string EditId { get; internal set; }

        public static void Start() {
            //Create keybinds
            keybinds.Add(new BookmarkKeybind());
            keybinds.Add(new RecordingKeybind());

            //Create hook
            keyboardDele = OnKeyEvent;
            keyboardHook = SetWindowsHookEx(13, keyboardDele, 0, 0);
            Logger.WriteLine("Loaded KeyboardHook...");
        }

        public static void Stop() {
            keybinds.Clear();
            UnhookWindowsHookEx(keyboardHook);
            Logger.WriteLine("Unloaded KeyboardHook...");
        }

        private static IntPtr OnKeyEvent(int Code, IntPtr W, IntPtr L) {
            if (Code < 0)
                return CallNextHookEx(keyboardHook, Code, W, L);

            try {
                KeyEvents kEvent = (KeyEvents)W;
                Keys vkCode = (Keys)Marshal.ReadInt32(L);
                if (kEvent == KeyEvents.KeyDown || kEvent == KeyEvents.SysKeyDown) {
                    pressedKeys.Add(vkCode);
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
                    cachePressedKeys.Add(vkCode);
                }
                else if (kEvent == KeyEvents.KeyUp || kEvent == KeyEvents.SysKeyUp) {
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
                    pressedKeys.Remove(vkCode);
                    cachePressedKeys.Remove(vkCode);
                }
            }
            catch (Exception e) {
                Logger.WriteLine("Error getting current keypress: " + e.ToString());
            }

            return CallNextHookEx(keyboardHook, Code, W, L);
        }

        public enum KeyEvents {
            KeyDown = 0x0100,
            KeyUp = 0x0101,
            SysKeyDown = 0x0104,
            SysKeyUp = 0x0105,
        }
    }
}
#endif
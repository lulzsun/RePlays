using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RePlays.Classes.Services.Hotkeys;

namespace RePlays.Services
{
    public static class HotkeyService
    {
        public delegate IntPtr CallbackDelegate(int Code, IntPtr W, IntPtr L);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct KBDLLHookStruct
        {
            public Int32 vkCode;
            public Int32 scanCode;
            public Int32 flags;
            public Int32 time;
            public Int32 dwExtraInfo;
        }

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr SetWindowsHookEx(int idHook, CallbackDelegate lpfn, int hInstance, int threadId);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(IntPtr idHook);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetCurrentThreadId();
        public static string EditId = null;
        private static List<Hotkey> _hotkeys = new();
        private static IntPtr HookID;
        static CallbackDelegate TheHookCB = null;

        public static void Start()
        { 
            //Create hotkeys
            _hotkeys.Add(new BookmarkHotkey());
            _hotkeys.Add(new RecordingHotkey());
            //Create hook
            TheHookCB = KeybHookProc;
            HookID = SetWindowsHookEx(13, TheHookCB, 0, 0);
            Logger.WriteLine("Loaded KeyboardHook...");
        }

        public static void Stop()
        {
            _hotkeys.Clear();
            UnhookWindowsHookEx(HookID);
            Logger.WriteLine("Unloaded KeyboardHook...");
        }

        private static IntPtr KeybHookProc(int Code, IntPtr W, IntPtr L)
        {
            if (Code < 0)
                return CallNextHookEx(HookID, Code, W, L);

            try
            {
                KeyEvents kEvent = (KeyEvents)W;
                if (kEvent == KeyEvents.KeyDown)
                {
                    Keys vkCode = (Keys)Marshal.ReadInt32(L);
                    vkCode |= Control.ModifierKeys;
                    foreach (Hotkey h in _hotkeys)
                    {
                        if (vkCode == h.Keybind) h.Action();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteLine("Error getting current keypress: " + e.ToString());
            }

            return CallNextHookEx(HookID, Code, W, L);
        }


        public enum KeyEvents
        {
            KeyDown = 0x0100,
            KeyUp = 0x0101
        }
    }
}

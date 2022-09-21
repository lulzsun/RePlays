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
        public delegate int CallbackDelegate(int Code, int W, int L);

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
        private static extern int SetWindowsHookEx(int idHook, CallbackDelegate lpfn, int hInstance, int threadId);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(int idHook, int nCode, int wParam, int lParam);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetCurrentThreadId();
        public static string EditId = null;
        private static List<Hotkey> _hotkeys = new();
        private static int HookID = 0;
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

        private static int KeybHookProc(int Code, int W, int L)
        {
            if (Code < 0)
                return CallNextHookEx(HookID, Code, W, L);

            try
            {
                KeyEvents kEvent = (KeyEvents)W;
                if (kEvent == KeyEvents.KeyDown)
                {
                    foreach (Hotkey h in _hotkeys)
                    {
                        if (h.IsPressed()) h.Action();
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

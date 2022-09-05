using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RePlays.Classes.Services
{
    public static class KeyboardHookService
    {

        public delegate void LocalKeyEventHandler(Keys key, bool Shift, bool Ctrl, bool Alt);

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

        private static int HookID = 0;
        static CallbackDelegate TheHookCB = null;

        public static void Start()
        {
            //Set bookmark key
            BookmarkService.SetBookmarkKeyFromSettings();

            //Create hook
            TheHookCB = new CallbackDelegate(KeybHookProc);
            HookID = SetWindowsHookEx(13, TheHookCB, 0, 0);
            Logger.WriteLine("Loaded KeyboardHook...");
        }

        public static void Stop(string videoName)
        {
            UnhookWindowsHookEx(HookID);
            BookmarkService.ApplyBookmarkToSavedVideo(videoName);
            Logger.WriteLine("Unloaded KeyboardHook...");
        }

        private static int KeybHookProc(int Code, int W, int L)
        {
            if (Code < 0)
                return CallNextHookEx(HookID, Code, W, L);

            try
            {
                KeyEvents kEvent = (KeyEvents)W;
                if (kEvent == KeyEvents.KeyUp)
                {
                    bool pressingBookmarkKey = BookmarkService.IsPressingBookmark();
                    if (pressingBookmarkKey)
                    {
                        BookmarkService.AddBookmark();
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
            KeyUp = 0x0101,
        }
    }
}

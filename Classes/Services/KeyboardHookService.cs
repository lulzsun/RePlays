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
        private static Keys bookmarkKey = Keys.F8;
        private static int latestBookmarkButtonPress = 0;

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
        static List<int> bookmarks = new List<int>();

        public static void Start()
        {

            //Get bookmark key
            string[] keybind;
            SettingsService.Settings.keybindings.TryGetValue("CreateBookmark", out keybind);
            for (int i = 0; i < keybind.Length; i++)
            {
                Keys key = Keys.None;
                Enum.TryParse(keybind[i], out key);
                if (i == 0) bookmarkKey = key;
                else bookmarkKey = key;

                //TODO: Make it possible to use multiple keys
                //else bookmarkKey |= key;
            }

            //Create hook
            TheHookCB = new CallbackDelegate(KeybHookProc);
            HookID = SetWindowsHookEx(13, TheHookCB, 0, 0);
            Logger.WriteLine("Loaded KeyboardHook...");
        }

        public static void Stop(string videoName)
        {
            UnhookWindowsHookEx(HookID);
            WebMessage.SetBookmarks(videoName, bookmarks, RecordingService.lastVideoDuration);
            bookmarks.Clear();
            Logger.WriteLine("Unloaded KeyboardHook...");
        }

        private static int KeybHookProc(int Code, int W, int L)
        {
            if (Code < 0)
            {
                return CallNextHookEx(HookID, Code, W, L);
            }
            try
            {
                KeyEvents kEvent = (KeyEvents)W;
                if (kEvent == KeyEvents.KeyUp)
                {
                    bool pressingBookmarkKey = IsPressingBookmark();
                    if (pressingBookmarkKey)
                    {
                        TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                        int secondsSinceEpoch = (int)t.TotalSeconds;
                        if (secondsSinceEpoch - latestBookmarkButtonPress >= 2)
                        {
                            latestBookmarkButtonPress = secondsSinceEpoch;
                            Logger.WriteLine("Bookmark: " + RecordingService.recordingElapsed);
                            bookmarks.Add(RecordingService.recordingElapsed);

                            System.IO.Stream soundStream = Properties.Resources.bookmark;
                            System.Media.SoundPlayer bookmarkSound = new System.Media.SoundPlayer(soundStream);
                            bookmarkSound.Play();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteLine(e.ToString());
            }

            return CallNextHookEx(HookID, Code, W, L);
        }

        public enum KeyEvents
        {
            KeyUp = 0x0101,
        }

        [DllImport("user32.dll")]
        static public extern short GetKeyState(Keys nVirtKey);

        public static bool IsPressingBookmark()
        {
            int state = GetKeyState(bookmarkKey);
            if (state > 1 || state < -1) return true;
            return false;
        }
    }
}

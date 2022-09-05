using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RePlays.Classes.Services
{
    internal static class BookmarkService
    {
        private static Keys bookmarkKey = Keys.F8;
        static List<int> bookmarks = new List<int>();
        static int latestBookmarkKeyPress = 0;

        public static void AddBookmark()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int secondsSinceEpoch = (int)t.TotalSeconds;

            if (secondsSinceEpoch - latestBookmarkKeyPress >= 2)
            {
                latestBookmarkKeyPress = secondsSinceEpoch;
                Logger.WriteLine("Adding bookmark: " + RecordingService.recordingElapsed);
                bookmarks.Add(RecordingService.recordingElapsed);

                System.IO.Stream soundStream = Properties.Resources.bookmark;
                System.Media.SoundPlayer bookmarkSound = new System.Media.SoundPlayer(soundStream);
                bookmarkSound.Play();
            }
        }

        public static void ApplyBookmarkToSavedVideo(string videoName)
        {
            WebMessage.SetBookmarks(videoName, bookmarks, RecordingService.lastVideoDuration);
            bookmarks.Clear();
        }

        public static void SetBookmarkKeyFromSettings()
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

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
        static List<int> bookmarks = new();
        static int latestBookmarkKeyPress;

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


    }
}

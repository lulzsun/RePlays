using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RePlays.Services
{
    internal static class BookmarkService
    {
        static List<Bookmark> bookmarks = new();
        static int latestBookmarkKeyPress;

        public static void AddBookmark(Bookmark bookmark)
        {
            int secondsSinceEpoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            if ((secondsSinceEpoch - latestBookmarkKeyPress >= 2) || !bookmark.type.Equals(Bookmark.BookmarkType.Manual))
            {
                latestBookmarkKeyPress = secondsSinceEpoch;
                double bookmarkTimestamp = RecordingService.GetTotalRecordingTimeInSecondsWithDecimals();
                Logger.WriteLine("Adding bookmark: " + bookmarkTimestamp);
                bookmark.time = bookmarkTimestamp;
                bookmarks.Add(bookmark);

                if(bookmark.type.Equals(Bookmark.BookmarkType.Manual)){
                    System.IO.Stream soundStream = Properties.Resources.bookmark;
                    System.Media.SoundPlayer bookmarkSound = new System.Media.SoundPlayer(soundStream);
                    bookmarkSound.Play();
                }
            }
        }

        public static void ApplyBookmarkToSavedVideo(string videoName)
        {
            try
            {
                Logger.WriteLine($"Applying bookmarks");
                WebMessage.SetBookmarks(videoName, bookmarks, RecordingService.lastVideoDuration);
                bookmarks.Clear();
                Logger.WriteLine($"Bookmark status [Successfully]");
            }
            catch (Exception e)
            {
                Logger.WriteLine($"Bookmark status [Failed] with exception {e.Message}");
            }
        }
    }

    public class Bookmark
    {
        public enum BookmarkType
        {
            Manual,
            Kill
        }
        public BookmarkType type { get; set; }
        public double time { get; set; }
    }
}

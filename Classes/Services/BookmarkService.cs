using RePlays.Utils;
using System;
using System.Collections.Generic;

namespace RePlays.Services {
    internal static class BookmarkService {
        static List<Bookmark> bookmarks = new();
        static int latestBookmarkKeyPress;

        public static void AddBookmark(Bookmark bookmark, DateTime? dateTime = null) {
            if (dateTime == null) {
                dateTime = DateTime.Now;
            }
            int secondsSinceEpoch = (int)(dateTime.Value - new DateTime(1970, 1, 1)).TotalSeconds;

            if ((secondsSinceEpoch - latestBookmarkKeyPress >= 2) || !bookmark.type.Equals(Bookmark.BookmarkType.Manual)) {
                latestBookmarkKeyPress = secondsSinceEpoch;
                double bookmarkTimestamp = RecordingService.GetTotalRecordingTimeInSecondsWithDecimals(dateTime);
                Logger.WriteLine("Adding bookmark: " + bookmarkTimestamp);
                bookmark.time = bookmarkTimestamp;
                bookmarks.Add(bookmark);

                if (bookmark.type.Equals(Bookmark.BookmarkType.Manual)) {
#if WINDOWS
                    System.Media.SoundPlayer bookmarkSound = new(Functions.GetResourcesFolder() + "bookmark.wav");
                    bookmarkSound.Play();
#endif
                }
            }
        }

        public static void ApplyBookmarkToSavedVideo(string videoName) {
            Logger.WriteLine($"Applying {bookmarks.Count} bookmarks");
            if (bookmarks.Count == 0) return;

            try {
                WebMessage.SetBookmarks(videoName, bookmarks, RecordingService.lastVideoDuration);
                bookmarks.Clear();
            }
            catch (Exception e) {
                Logger.WriteLine($"Bookmark status: Failed with exception {e.Message}");
            }
        }
    }

    public class Bookmark {
        public enum BookmarkType {
            Manual,
            Kill,
            Death,
            Assist
        }
        public BookmarkType type { get; set; }
        public double time { get; set; }
    }
}

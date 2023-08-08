﻿#if WINDOWS
using RePlays.Services;

namespace RePlays.Classes.Services.Keybinds {
    public class BookmarkKeybind : Keybind {
        public BookmarkKeybind() {
            Id = "CreateBookmark";
            DefaultKeys = new string[] { "F8" };
            SetKeybind();
        }
        public override void Action() {
            if (RecordingService.IsRecording) BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Manual });
        }
    }
}
#endif
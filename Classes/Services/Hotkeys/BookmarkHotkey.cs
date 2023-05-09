using RePlays.Services;

namespace RePlays.Classes.Services.Hotkeys {
    public class BookmarkHotkey : Hotkey {
        private readonly string key = "CreateBookmark";
        public override void Action() {
            if (RecordingService.IsRecording) BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Manual });
        }

        protected override void SetKeybind() {
            string[] keybind;
            SettingsService.Settings.keybindings.TryGetValue(key, out keybind);
            _keybind = ParseKeys(key, keybind);
        }
    }
}

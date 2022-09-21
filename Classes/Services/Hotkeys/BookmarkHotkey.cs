using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RePlays.Services;

namespace RePlays.Classes.Services.Hotkeys
{
    public class BookmarkHotkey : Hotkey
    {
        public override void Action()
        {
            if (RecordingService.IsRecording) BookmarkService.AddBookmark();
        }

        protected override void SetKeybind()
        {
            string[] keybind;
            SettingsService.Settings.keybindings.TryGetValue("CreateBookmark", out keybind);
            _keybind = ParseKeys(keybind);
        }
    }
}

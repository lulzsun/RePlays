using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RePlays.Services;

namespace RePlays.Classes.Services.Hotkeys
{
    public class RecordingHotkey : Hotkey
    {
        private readonly string key = "StartStopRecording";
        public override void Action()
        {
            if (RecordingService.IsRecording) RecordingService.StopRecording();
            else RecordingService.StartRecording();
        }

        protected override void SetKeybind()
        {
            string[] keybind;
            SettingsService.Settings.keybindings.TryGetValue(key, out keybind);
            _keybind = ParseKeys(key, keybind);
        }
    }
}

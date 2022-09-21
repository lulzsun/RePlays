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
        public override void Action()
        {
            if (RecordingService.IsRecording) RecordingService.StopRecording();
            else RecordingService.StartRecording();
        }

        protected override void SetKeybind()
        {
            string[] keybind;
            SettingsService.Settings.keybindings.TryGetValue("StartStopRecording", out keybind);
            _keybind = ParseKeys(keybind);
        }
    }
}

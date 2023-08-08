#if WINDOWS
using RePlays.Services;

namespace RePlays.Classes.Services.Keybinds {
    public class RecordingKeybind : Keybind {
        public RecordingKeybind() {
            Id = "StartStopRecording";
            DefaultKeys = new string[] { "LControlKey", "F9" };
            SetKeybind();
        }
        public override void Action() {
            if (RecordingService.IsRecording) RecordingService.StopRecording();
            else RecordingService.StartRecording();
        }
    }
}
#endif
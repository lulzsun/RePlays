using RePlays.Services;

namespace RePlays.Classes.Services.Keybinds {
    public class RecordingKeybind : Keybind {
        public RecordingKeybind() {
            Id = "StartStopRecording";
            DefaultKeys = ["LeftControl", "F9"];
            SetKeybind();
        }
        public override void Action() {
            if (RecordingService.IsRecording) RecordingService.StopRecording(true);
            else RecordingService.StartRecording(true);
        }
    }
}
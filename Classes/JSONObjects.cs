using System;
using System.Collections.Generic;
using System.IO;

namespace RePlays.JSONObjects {
    public class VideoList {
        public string game { get; set; }
        public List<string> games { get; set; }
        public string sortBy { get; set; }
        public List<Video> sessions { get; set; }
        public long sessionsSize { get; set; }
        public List<Video> clips { get; set; }
        public long clipsSize { get; set; }
    }

    public class Video {
        public DateTime date { get; set; }
        public string type { get; set; }
        public long size { get; set; }
        public string game { get; set; }
        public string fileName { get; set; }
        public string thumbnail { get; set; }
    }

    public class VideoSortSettings {
        public string game { get; set; }
        public string sortBy { get; set; }
    }

    public class ClipSegment {
        public float start { get; set; }
        public float duration { get; set; }
    }

    // Settings Objects
    public class GeneralSettings {
        private bool _launchStartup = true;
        public bool launchStartup { get { return _launchStartup; } set { _launchStartup = value; } }
        private bool _startMinimized = false;
        public bool startMinimized { get { return _startMinimized; } set { _startMinimized = value; } }
        private string _theme = "System";
        public string theme { get { return _theme; } set { _theme = value; } }
        private string _update = "automatic"; // ??? why is there a warning
        public string update { get { return _update; } set { _update = value; } }
    }

    public class MicDevice {
        private string _deviceId = "";
        public string deviceId { get { return _deviceId; } set { _deviceId = value; } }
        private string _deviceLabel = "";
        public string deviceLabel { get { return _deviceLabel; } set { _deviceLabel = value; } }
    }

    public class CaptureSettings {
        private string _recordingMode = "automatic";
        public string recordingMode { get { return _recordingMode; } set { _recordingMode = value; } }
        private int _resolution = 1080;
        public int resolution { get { return _resolution; } set { _resolution = value; } }
        private int _frameRate = 60;
        public int frameRate { get { return _frameRate; } set { _frameRate = value; } }
        private int _bitRate = 50;
        public int bitRate { get { return _bitRate; } set { _bitRate = value; } }

        private int _gameAudioVolume = 100;
        public int gameAudioVolume { get { return _gameAudioVolume; } set { _gameAudioVolume = value; } }
        private int _micAudioVolume = 50;
        public int micAudioVolume { get { return _micAudioVolume; } set { _micAudioVolume = value; } }

        private MicDevice _micDevice = new();
        public MicDevice micDevice { get { return _micDevice; } set { _micDevice = value; } }

        private string _videoSaveDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Plays");
        public string videoSaveDir { get { return _videoSaveDir; } set { _videoSaveDir = value; } }
        private string _tempSaveDir = Path.Join(Path.GetTempPath(), "Plays");
        public string tempSaveDir { get { return _tempSaveDir; } set { _tempSaveDir = value; } }
    }
}
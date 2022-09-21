using Squirrel;
using System;
using System.Collections.Generic;
using System.IO;

namespace RePlays.Utils {
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
        public long size { get; set; }
        public string game { get; set; }
        public string fileName { get; set; }
        public string thumbnail { get; set; }
        public string folder { get; set; }
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
        public bool launchStartup { get { return _launchStartup; } 
            set { 
                _launchStartup = value;
                try {
                    using (var manager = new UpdateManager(Environment.GetEnvironmentVariable("LocalAppData") + @"\RePlays\packages")) {
                        if (_launchStartup == true)
                            manager.CreateShortcutsForExecutable("RePlays.exe", ShortcutLocation.Startup, false);
                        else
                            manager.RemoveShortcutsForExecutable("RePlays.exe", ShortcutLocation.Startup);
                    }
                }
                catch (Exception exception) {
                    Logger.WriteLine("Error: Issue editing program startup setting: " + exception.ToString());
                }
            }
        }
        private bool _startMinimized = false;
        public bool startMinimized { get { return _startMinimized; } set { _startMinimized = value; } }
        private string _theme = "System";
        public string theme { get { return _theme; } set { _theme = value; } }
        private string _update = "automatic"; // ??? why is there a warning
        public string update { get { return _update; } set { _update = value; } }
        private string _updateChannel = "stable";
        public string updateChannel { get { return _updateChannel; } set { _updateChannel = value; } }
        public string currentVersion { get { return Updater.currentVersion; } }
        public string latestVersion { get { return Updater.latestVersion; } }
    }

    public class AudioDevice {
        public AudioDevice() { }
        public AudioDevice(string deviceId, string deviceLabel) { 
            _deviceId = deviceId;
            _deviceLabel = deviceLabel;
            }
        private string _deviceId = "";
        public string deviceId { get { return _deviceId; } set { _deviceId = value; } }
        private string _deviceLabel = "";
        public string deviceLabel { get { return _deviceLabel; } set { _deviceLabel = value; } }
    }

    public class CaptureSettings {
        private string _recordingMode = "automatic";
        public string recordingMode { get { return _recordingMode; } set { _recordingMode = value; } }
        private List<string> _encodersCache = new();
        public List<string> encodersCache { get { return _encodersCache; } set { _encodersCache = value; } }
        private string _encoder = string.Empty;
        public string encoder { get { return _encoder; } set { _encoder = value; } }
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

        private List<AudioDevice> _inputDevicesCache = new();
        public List<AudioDevice> inputDevicesCache { get { return _inputDevicesCache; } set { _inputDevicesCache = value; } }

        private AudioDevice _inputDevice = new();
        public AudioDevice inputDevice { get { return _inputDevice; } set { _inputDevice = value; } }

        private List<AudioDevice> _outputDevicesCache = new();
        public List<AudioDevice> outputDevicesCache { get { return _outputDevicesCache; } set { _outputDevicesCache = value; } }

        private AudioDevice _outputDevice = new();
        public AudioDevice outputDevice { get { return _outputDevice; } set { _outputDevice = value; } }
    }

    public class StorageSettings {
        private string _videoSaveDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Plays");
        public string videoSaveDir { get { return _videoSaveDir; } set { _videoSaveDir = value; } }
        private string _tempSaveDir = Path.Join(Path.GetTempPath(), "Plays");
        public string tempSaveDir { get { return _tempSaveDir; } set { _tempSaveDir = value; } }
        public List<string> _extraVideoSaveDir = new();
        public List<string> extraVideoSaveDir { get { return _extraVideoSaveDir; } set { _extraVideoSaveDir = value; } }

        public bool _autoManageSpace = true;
        public bool autoManageSpace { get { return _autoManageSpace; } set { _autoManageSpace = value; } }
        public int _manageSpaceLimit = -1;
        public int manageSpaceLimit { get { return _manageSpaceLimit; } set { _manageSpaceLimit = value; } }
        public int _manageTimeLimit = -1;
        public int manageTimeLimit { get { return _manageTimeLimit; } set { _manageTimeLimit = value; } }
    }

    public class UploadSettings {
        private List<string> _recentLinks = new();
        public List<string> recentLinks { get { return _recentLinks; } set { _recentLinks = value; } }

        public class StreamableSettings {
            private string _email = "";
            public string email { get { return _email; } set { _email = value; } }
            private string _password = "";
            public string password { get { return _password; } set { _password = value; } }
        }

        private StreamableSettings _streamableSettings = new();
        public StreamableSettings streamableSettings { get { return _streamableSettings; } set { _streamableSettings = value; } }

        public class LocalFolderSettings {
            private string _dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            public string dir { get { return _dir; } set { _dir = value; } }
        }

        private LocalFolderSettings _localFolderSettings = new();
        public LocalFolderSettings localFolderSettings { get { return _localFolderSettings; } set { _localFolderSettings = value; } }
    }

    public class DetectionSettings {
        private List<CustomGame> _whitelist = new();
        public List<CustomGame> whitelist { get { return _whitelist; } set { _whitelist = value; } }
        private List<string> _blacklist = new();
        public List<string> blacklist { get { return _blacklist; } set { _blacklist = value; } }
    }

    public struct CustomGame
    {
        public CustomGame(string gameExe, string gameName)
        {
            this._gameExe = gameExe;
            this._gameName = gameName;
        }

        private string _gameExe;
        public string gameExe { get { return _gameExe;} set { _gameExe = value; } }
        private string _gameName;
        public string gameName { get { return _gameName; } set { _gameName = value; } }
    }
}
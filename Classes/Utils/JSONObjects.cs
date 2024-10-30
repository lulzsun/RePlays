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
        public List<Video> corrupted { get; set; }
    }

    public class Video {
        public DateTime date { get; set; }
        public long size { get; set; }
        public string game { get; set; }
        public string fileName { get; set; }
        public string thumbnail { get; set; }
        public string folder { get; set; }
        public VideoMetadata metadata { get; set; }
    }

    public class VideoMetadata {
        public double duration { get; set; }
        public int kills { get; set; }
        public int assists { get; set; }
        public int deaths { get; set; }
        public string champion { get; set; }
        public bool? win { get; set; }
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
        public bool launchStartup {
            get { return _launchStartup; }
            set {
                _launchStartup = value;
#if WINDOWS
                try {
                    using (var manager = new Squirrel.UpdateManager(Environment.GetEnvironmentVariable("LocalAppData") + @"\RePlays\packages")) {
                        if (_launchStartup == true)
                            manager.CreateShortcutsForExecutable("RePlays.exe", Squirrel.ShortcutLocation.Startup, false);
                        else
                            manager.RemoveShortcutsForExecutable("RePlays.exe", Squirrel.ShortcutLocation.Startup);
                    }
                }
                catch (Exception exception) {
                    Logger.WriteLine("Error: Issue editing program startup setting: " + exception.ToString());
                }
#endif
            }
        }
        private bool _startMinimized = false;
        public bool startMinimized { get { return _startMinimized; } set { _startMinimized = value; } }
        private string _theme = "System";
        public string theme { get { return _theme; } set { _theme = value; } }
        private string _update = "automatic"; // ??? why is there a warning
        public string update { get { return _update; } set { _update = value; } }
        private string _updateChannel = "Stable";
        public string updateChannel { get { return _updateChannel; } set { _updateChannel = value; } }
        public string currentVersion { get { return Updater.currentVersion; } }
        public string latestVersion { get { return Updater.latestVersion; } }
        private Device _device = new();
        public Device device { get { return _device; } set { _device = value; } }
        private string _language = "en";
        public string language { get { return _language; } set { _language = value; } }

    }

    public class Device {
        private string? _gpuManufacturer;
        public string gpuManufacturer { get { return _gpuManufacturer; } set { _gpuManufacturer = value; } }
    }

    public class AudioDevice {
        public AudioDevice() { }
        public AudioDevice(string deviceId, string deviceLabel, bool denoiser = false) {
            _deviceId = deviceId;
            _deviceLabel = deviceLabel;
            _denoiser = denoiser;
        }
        private string _deviceId = "";
        public string deviceId { get { return _deviceId; } set { _deviceId = value; } }
        private string _deviceLabel = "";
        public string deviceLabel { get { return _deviceLabel; } set { _deviceLabel = value; } }
        private int _deviceVolume = 100;
        public int deviceVolume { get { return _deviceVolume; } set { _deviceVolume = value; } }
        private bool _denoiser;
        public bool denoiser { get { return _denoiser; } set { _denoiser = value; } }
    }

    public class AudioApplication {
        public AudioApplication() { }
        public AudioApplication(string application) {
            _application = application;
        }

        private string _application;
        public string application { get { return _application; } set { _application = value; } }
        private int _applicationVolume = 100;
        public int applicationVolume { get { return _applicationVolume; } set { _applicationVolume = value; } }
    }

    public class CaptureSettings {
        private string _recordingMode = "automatic";
        public string recordingMode { get { return _recordingMode; } set { _recordingMode = value; } }

        private bool _useDisplayCapture = true;
        public bool useDisplayCapture { get { return _useDisplayCapture; } set { _useDisplayCapture = value; } }

        private bool _useRecordingStartSound = true;
        public bool useRecordingStartSound { get { return _useRecordingStartSound; } set { _useRecordingStartSound = value; } }

        private List<string> _encodersCache = new();
        /// <summary>
        /// TODO: Remove cache in user settings, this is not good practice
        /// </summary>
        public List<string> encodersCache { get { return _encodersCache; } set { _encodersCache = value; } }

        private string _encoder = string.Empty;
        public string encoder { get { return _encoder; } set { _encoder = value; } }

        private string _rateControl = string.Empty;

        private List<string> _rateControlCache = new();
        /// <summary>
        /// TODO: Remove cache in user settings, this is not good practice
        /// </summary>
        public List<string> rateControlCache { get { return _rateControlCache; } set { _rateControlCache = value; } }

        public string rateControl { get { return _rateControl; } set { _rateControl = value; } }

        private int _maxScreenResolution = 1080;
        public int maxScreenResolution { get { return _maxScreenResolution; } set { _maxScreenResolution = value; } }

        private int _resolution = 1080;
        public int resolution { get { return _resolution; } set { _resolution = value; } }

        private int _frameRate = 60;
        public int frameRate { get { return _frameRate; } set { _frameRate = value; } }

        private int _bitRate = 50;
        public int bitRate { get { return _bitRate; } set { _bitRate = value; } }

        private int _maxBitRate = 50;
        public int maxBitRate { get { return _maxBitRate; } set { _maxBitRate = value; } }

        private int _cqLevel = 20;
        public int cqLevel { get { return _cqLevel; } set { _cqLevel = value; } }

        private List<AudioDevice> _inputDevicesCache = new();
        /// <summary>
        /// TODO: Remove cache in user settings, this is not good practice
        /// </summary>
        public List<AudioDevice> inputDevicesCache { get { return _inputDevicesCache; } set { _inputDevicesCache = value; } }

        private List<AudioDevice> _inputDevices = new();
        public List<AudioDevice> inputDevices { get { return _inputDevices; } set { _inputDevices = value; } }

        /// <summary>
        /// TODO: Remove cache in user settings, this is not good practice
        /// </summary>
        private List<AudioDevice> _outputDevicesCache = new();
        public List<AudioDevice> outputDevicesCache { get { return _outputDevicesCache; } set { _outputDevicesCache = value; } }

        private List<AudioDevice> _outputDevices = new();
        public List<AudioDevice> outputDevices { get { return _outputDevices; } set { _outputDevices = value; } }

        public List<AudioApplication> _audioApplications = new();
        public List<AudioApplication> audioApplications { get { return _audioApplications; } set { _audioApplications = value; } }

        private bool _hasNvidiaAudioSDK;
        public bool hasNvidiaAudioSDK { get { return _hasNvidiaAudioSDK; } set { _hasNvidiaAudioSDK = value; } }

        private List<FileFormat> _fileFormatCache = new() { new FileFormat("mp4", "MPEG-4 (.mp4)", true) }; // Initially set to MP4, Updated inside LibOBSRecorder when loaded.
        /// <summary>
        /// TODO: Remove cache in user settings, this is not good practice
        /// </summary>
        public List<FileFormat> fileFormatsCache { get { return _fileFormatCache; } set { _fileFormatCache = value; } }

        private FileFormat _fileFormat = new FileFormat("fragmented_mp4", "Fragmented MP4 (.mp4)", true);
        public FileFormat fileFormat { get { return _fileFormat; } set { _fileFormat = value; } }

        private bool _useReplayBuffer = false;
        public bool useReplayBuffer { get { return _useReplayBuffer; } set { _useReplayBuffer = value; } }
        // In seconds

        private uint _replayBufferDuration = 30;
        public uint replayBufferDuration { get { return _replayBufferDuration; } set { _replayBufferDuration = value; } }

        // In MB
        private uint _replayBufferSize = 500;
        public uint replayBufferSize { get { return _replayBufferSize; } set { _replayBufferSize = value; } }

        private bool _captureGameAudio = false;
        public bool captureGameAudio { get { return _captureGameAudio; } set { _captureGameAudio = value; } }
    }

    public class ClipSettings {
        private bool _reEncode = false;
        public bool reEncode { get { return _reEncode; } set { _reEncode = value; } }
        private string _renderHardware = "CPU";
        public string renderHardware { get { return _renderHardware; } set { _renderHardware = value; } }
        private uint _renderQuality = 23;
        public uint renderQuality { get { return _renderQuality; } set { _renderQuality = value; } }
        private string _renderCodec = "libx264";
        public string renderCodec { get { return _renderCodec; } set { _renderCodec = value; } }
        private uint? _renderCustomFps;
        public uint? renderCustomFps { get { return _renderCustomFps; } set { _renderCustomFps = value; } }
    }

    public class FileFormat {
        public string title { get; }
        public string format { get; }
        public bool isReplayBufferCompatible { get; }

        public FileFormat(string format, string title, bool isReplayBufferCompatible) {
            this.format = format;
            this.title = title;
            this.isReplayBufferCompatible = isReplayBufferCompatible;
        }

        public override string ToString() {
            return $"File Format {{Format: {format}, Title: {title}}}";
        }

        public string GetFileExtension() {
            return this.format.Replace("fragmented_", "");
        }
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
        private bool _openAfterUpload = true;
        public bool openAfterUpload { get { return _openAfterUpload; } set { _openAfterUpload = value; } }

        public class StreamableSettings {
            private string _email = "";
            public string email { get { return _email; } set { _email = value; } }
            private string _password = "";
            public string password { get { return _password; } set { _password = value; } }
        }

        private StreamableSettings _streamableSettings = new();
        public StreamableSettings streamableSettings { get { return _streamableSettings; } set { _streamableSettings = value; } }
        public class RePlaysSettings {
            private string _email = "";
            public string email { get { return _email; } set { _email = value; } }
            private string _password = "";
            public string password { get { return _password; } set { _password = value; } }
        }

        private RePlaysSettings _rePlaysSettings = new();
        public RePlaysSettings rePlaysSettings { get { return _rePlaysSettings; } set { _rePlaysSettings = value; } }

        public class LocalFolderSettings {
            private string _dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            public string dir { get { return _dir; } set { _dir = value; } }
        }

        private LocalFolderSettings _localFolderSettings = new();
        public LocalFolderSettings localFolderSettings { get { return _localFolderSettings; } set { _localFolderSettings = value; } }

        public class CustomUploaderSettings {

            private string _method = "POST";
            public string method { get { return _method; } set { _method = value; } }

            private string _url = "";
            public string url { get { return _url; } set { _url = value; } }

            private KeyValuePair<string, string>[] _headers = Array.Empty<KeyValuePair<string, string>>();
            public KeyValuePair<string, string>[] headers { get { return _headers; } set { _headers = value; } }

            private KeyValuePair<string, string>[] _urlparams = Array.Empty<KeyValuePair<string, string>>();
            public KeyValuePair<string, string>[] urlparams { get { return _urlparams; } set { _urlparams = value; } }

            private string _responseType = "TEXT";
            public string responseType { get { return _responseType; } set { _responseType = value; } }

            private string _responsePath = "";
            public string responsePath { get { return _responsePath; } set { _responsePath = value; } }


        }
        private CustomUploaderSettings _customUploaderSettings = new();
        public CustomUploaderSettings customUploaderSettings { get { return _customUploaderSettings; } set { _customUploaderSettings = value; } }


    }

    public class DetectionSettings {
        private List<CustomGame> _whitelist = new();
        public List<CustomGame> whitelist { get { return _whitelist; } set { _whitelist = value; } }
        private List<string> _blacklist = new();
        public List<string> blacklist { get { return _blacklist; } set { _blacklist = value; } }
    }

    public class KeybindSettings : Dictionary<string, CustomKeybind> { }

    public struct CustomKeybind {
        public CustomKeybind(bool disabled, string[] keys) {
            this._disabled = disabled;
            this._keys = keys;
        }
        private bool _disabled;
        public bool disabled { get { return _disabled; } set { _disabled = value; } }
        private string[] _keys;
        public string[] keys { get { return _keys; } set { _keys = value; } }
    }

    public struct CustomGame {
        public CustomGame(string gameExe, string gameName) {
            this._gameExe = gameExe;
            this._gameName = gameName;
        }

        private string _gameExe;
        public string gameExe { get { return _gameExe; } set { _gameExe = value; } }
        private string _gameName;
        public string gameName { get { return _gameName; } set { _gameName = value; } }
    }
}
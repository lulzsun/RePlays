using LibObs;
using RePlays.Integrations;
using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static LibObs.Obs;
using static RePlays.Utils.Functions;
using Rect = RePlays.Services.WindowService.Rect;

namespace RePlays.Recorders {
    public class LibObsRecorder : BaseRecorder {
        public bool Connected { get; private set; }
        public bool DisplayCapture;
        public bool isStopping;
        static string videoSavePath { get; set; } = "";
        static string videoNameTimeStamp = "";
        static IntPtr windowHandle = IntPtr.Zero;
        static IntPtr output = IntPtr.Zero;

        static Rect windowSize;

        Dictionary<string, IntPtr> audioSources = new(), videoSources = new();
        Dictionary<string, IntPtr> audioEncoders = new(), videoEncoders = new();

        private readonly Dictionary<string, string> videoEncoderIds = new() {
#if WINDOWS
            {"Software (x264)", "obs_x264"},
            {"Hardware (NVENC AV1)", "jim_av1_nvenc"},
            {"Hardware (NVENC HEVC)", "jim_hevc_nvenc"},
            {"Hardware (NVENC)", "jim_nvenc"},
            {"Hardware (QSV)", "obs_qsv11"},
            {"Hardware (AMF)", "amd_amf_h264"},
            {"Hardware (AMD AVC)", "h264_texture_amf"},
            {"Hardware (AMD HEVC)", "h265_texture_amf"},
            {"Hardware (AMD AV1)", "av1_texture_amf"}
#else
            {"Software (x264)", "obs_x264"},
            {"Hardware (FFMPEG VAAPI H.264)", "ffmpeg_vaapi"},
            {"Hardware (GSTREAMER VAAPI H.264)", "obs-va-vah264enc"},
            {"Hardware (FFMPEG VAAPI HEVC)", "hevc_ffmpeg_vaapi"},
#endif
        };

        private readonly Dictionary<string, string> rate_controls = new() {
            {"VBR", "VBR"},
            {"VBR_LAT", "VBR_LAT"},
            {"QVBR", "QVBR"},
            {"HQVBR", "HQVBR"},
            {"HQCBR", "HQCBR"},
            {"CBR", "CBR"},
            {"CQP", "CQP"},
            {"Lossless", "Lossless"},
            {"ABR", "ABR"},
            {"CRF", "CRF"},
        };

        private Dictionary<string, List<string>> videoEncoderLink = new() {
            { "Software (x264)", new List<string> { "VBR", "CBR", "CRF" } },
            { "Hardware (NVENC)", new List<string> { "VBR", "CBR", "CQP", "Lossless" } },
            { "Hardware (QSV)", new List<string> { "VBR", "CBR" } },
            { "Hardware (AMF)", new List<string> { "VBR", "CBR", "ABR", "CRF" } },
            { "Hardware (AMD AVC)", new List<string> { "VBR", "VBR_LAT", "QVBR", "HQVBR", "HQCBR", "CBR", "CQP" } },
            { "Hardware (AMD HEVC)", new List<string> { "VBR", "VBR_LAT", "QVBR", "HQVBR", "HQCBR", "CBR", "CQP" } },
            { "Hardware (AMD AV1)", new List<string> { "VBR", "VBR_LAT", "QVBR", "HQVBR", "HQCBR", "CBR", "CQP" } },
        };


        private readonly FileFormat file_format_default = new FileFormat("fragmented_mp4", "Fragmented MP4 (.mp4)", false);
        private List<FileFormat> file_formats = new() {
            new FileFormat("fragmented_mp4", "Fragmented MP4 (.mp4)", false),
            new FileFormat("fragmented_mov", "Fragmented MOV (.mov)", false),
            new FileFormat("flv", "Flash Video (.flv)", false),
            new FileFormat("mkv", "Matroska Video (.mkv)", true),
            new FileFormat("mpegts", "MPEG-TS (.ts)", false),
            new FileFormat("mp4", "MP4 (.mp4)", true),
            new FileFormat("mov", "QuickTime (.mov)", true)
        };

        static bool signalOutputStop = false;
        static bool signalGCHookSuccess = false;
        static int signalGCHookAttempt = 0;

        static signal_callback_t outputStopCb;

        [DllImport("libX11", EntryPoint = "XOpenDisplay")]
        public static extern IntPtr XOpenDisplay(IntPtr display);

        public override void Start() {
            if (Connected) return;

            // STARTUP
#if !WINDOWS
            string libobsPath = Path.Combine(Directory.GetCurrentDirectory(), "libobs.so");
            if (!File.Exists(libobsPath)) {
                throw new Exception("error: Missing libobs.so");
            }
            NativeLibrary.Load(libobsPath);
#endif
            if (obs_initialized()) {
                throw new Exception("error: obs already initialized");
            }

#if !WINDOWS
            obs_set_nix_platform(obs_nix_platform_type.OBS_NIX_PLATFORM_X11_EGL);
            obs_set_nix_platform_display(XOpenDisplay(IntPtr.Zero));
            base_set_log_handler(null, IntPtr.Zero);
#else
            // Warning: if you try to access methods/vars/etc. that are not static within the log handler,
            // it will cause a System.ExecutionEngineException, something to do with illegal memory
            //
            // Warning: This also does crashes on linux at a certain point. See https://github.com/dotnet/runtime/issues/48796
            base_set_log_handler(new log_handler_t((lvl, msg, args, p) => {
                try {
                    string formattedMsg = MarshalUtils.GetLogMessage(msg, args);
                    Logger.WriteLine(((LogErrorLevel)lvl).ToString() + ": " + formattedMsg);

                    // a very crude way to see if game_capture source has successfully hooked/capture application....
                    // does game_capture source provide any signals that we can alternatively use?
                    if (formattedMsg == "[game-capture: 'gameplay'] Starting capture") {
                        if (signalGCHookSuccess && RecordingService.IsRecording) {
                            // everytime the "Starting capture" signal occurs, there could be a possibility that the game window has resized
                            // check to see if windowSize is different from currentSize, if so, restart recording with correct output resolution
                            Rect currentSize = WindowService.GetWindowSize(windowHandle);
                            if ((currentSize.GetWidth() > 1 && currentSize.GetHeight() > 1) && // fullscreen tabbing check
                                (IsValidAspectRatio(currentSize.GetWidth(), currentSize.GetHeight())) && // if it is (assumed) valid game aspect ratio for recording
                                (currentSize.GetWidth() != windowSize.GetWidth() || currentSize.GetHeight() != windowSize.GetHeight())) {
                                RestartRecording();
                            }
                            else {
                                Logger.WriteLine("Fullscreen game coming into focus? Ignoring attempt to restart recording.");
                            }
                        }
                        Logger.WriteLine("Successful game capture hook!");
                        signalGCHookSuccess = true;
                    }
                    else if (formattedMsg == "[game-capture: 'gameplay'] capture stopped") {
                        signalGCHookSuccess = false;
                    }
                    else if (formattedMsg.StartsWith("[game-capture: 'gameplay'] attempting to hook")) {
                        Logger.WriteLine($"Waiting for successful graphics hook for... retry attempt #{signalGCHookAttempt}");
                        signalGCHookAttempt++;
                    }
                    else if (formattedMsg.Contains("No space left on device")) {
                        WebMessage.DisplayModal("No space left on " + SettingsService.Settings.storageSettings.videoSaveDir[..1] + ": drive. Please free up some space by deleting unnecessary files.", "Unable to save video", "warning");
                        RecordingService.StopRecording();
                    }
                }
                catch (Exception e) {
                    // something went wrong, most likely an issue with our sprintf implementation?
                    Logger.WriteLine(e.ToString());
                    Logger.WriteLine(e.StackTrace);
                }
            }), IntPtr.Zero);
#endif

            Logger.WriteLine("libobs version: " + obs_get_version_string());
            if (!obs_startup("en-US", null, IntPtr.Zero)) {
                throw new Exception("error on libobs startup");
            }
            obs_add_data_path("./data/libobs/");
            obs_add_module_path("./obs-plugins/64bit/", "./data/obs-plugins/%module%/");

            ResetAudio();
            ResetVideo();

            obs_load_all_modules();
            obs_log_loaded_modules();
            obs_post_load_modules();

            // Warning: if you try to access methods/vars/etc. that are not static within the log handler,
            // it will cause a System.ExecutionEngineException, something to do with illegal memory
            outputStopCb = new signal_callback_t((data, cd) => {
                signalOutputStop = true;
            });

            Connected = true;
            Logger.WriteLine("Successfully started LibObs!");

            // post libobs initialization
            GetAvailableEncoders();
            GetAvailableRateControls();
            HasNvidiaAudioSDK();
            GetAvailableFileFormats();

            // update user settings
            WebMessage.SendMessage(GetUserSettings());
        }

        const int retryInterval = 2000; // 2 second
        const int maxRetryAttempts = 20; // 30 retries
        public override async Task<bool> StartRecording() {
            if (output != IntPtr.Zero) return false;

            signalOutputStop = false;
            int retryAttempt = 0;
            var session = RecordingService.GetCurrentSession();

            // If session is empty, this is a manual record attempt. Lets try to yolo record the foregroundwindow
            if (session.Pid == 0 && WindowService.GetForegroundWindow(out int processId, out nint hwid)) {
                if (processId != 0 || hwid != 0) {
                    WindowService.GetExecutablePathFromProcessId(processId, out string executablePath);
                    DetectionService.AutoDetectGame(processId, executablePath, hwid, autoRecord: false);
                    session = RecordingService.GetCurrentSession();
                }
                else {
                    return false;
                }
            }

            // attempt to retrieve process's window handle to retrieve class name and window title
            windowHandle = session.WindowHandle;
            while ((DetectionService.HasBadWordInClassName(windowHandle) || windowHandle == IntPtr.Zero) && retryAttempt < maxRetryAttempts) {
                Logger.WriteLine($"Waiting to retrieve process handle... retry attempt #{retryAttempt}");
                await Task.Delay(retryInterval);
                retryAttempt++;
                // alternate on retry attempts, one or the other might get us a better handle
                windowHandle = WindowService.GetWindowHandleByProcessId(session.Pid, retryAttempt % 2 == 1);
            }
            if (retryAttempt >= maxRetryAttempts) {
                return false;
            }
            retryAttempt = 0;

            string dir = Path.Join(GetPlaysFolder(), "/" + MakeValidFolderNameSimple(session.GameTitle) + "/");
            try {
                Directory.CreateDirectory(dir);
            }
            catch (Exception e) {
                WebMessage.DisplayModal(string.Format("Unable to create folder {0}. Do you have permission to create it?", dir), "Recording Error", "warning");
                Logger.WriteLine(e.ToString());
                return false;
            }
            videoNameTimeStamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");

            FileFormat currentFileFormat = SettingsService.Settings.captureSettings.fileFormat ?? (new FileFormat("mp4", "MP4 (.mp4)", true));
            Logger.WriteLine($"Output file format: " + currentFileFormat.ToString());
            videoSavePath = Path.Join(dir, videoNameTimeStamp + "-ses." + currentFileFormat.GetFileExtension());

            // Get the window class name
#if WINDOWS
            var windowClassNameId = WindowService.GetWindowTitle(windowHandle) + ":" + WindowService.GetClassName(windowHandle) + ":" + Path.GetFileName(session.Exe);
#else
            var windowClassNameId = windowHandle + "\r\n" + WindowService.GetWindowTitle(windowHandle) + "\r\n" + WindowService.GetClassName(windowHandle);
#endif
            // get game's window size and change output to match
            windowSize = WindowService.GetWindowSize(windowHandle);
            // sometimes, the inital window size might be in a middle of a transition, and gives us a weird dimension
            // this is a quick a dirty check: if there aren't more than 1120 pixels, we can assume it needs a retry
            while (windowSize.GetWidth() + windowSize.GetHeight() < 1120 && retryAttempt < maxRetryAttempts) {
                Logger.WriteLine($"Waiting to retrieve correct window size (currently {windowSize.GetWidth()}x{windowSize.GetHeight()})... retry attempt #{retryAttempt}");
                await Task.Delay(retryInterval);
                retryAttempt++;
                windowSize = WindowService.GetWindowSize(windowHandle);
            }
            if (windowSize.GetWidth() + windowSize.GetHeight() < 1120 && retryAttempt >= maxRetryAttempts) {
                Logger.WriteLine($"Possible issue in getting correct window size {windowSize.GetWidth()}x{windowSize.GetHeight()}");
                ResetVideo();
            }
            else {
                Logger.WriteLine($"Game capture window size: {windowSize.GetWidth()}x{windowSize.GetHeight()}");
                ResetVideo(windowHandle, windowSize.GetWidth(), windowSize.GetHeight());
            }

            Logger.WriteLine($"Preparing to create libobs output [{bnum_allocs()}]...");
#if WINDOWS
            string audioOutSourceId = "wasapi_output_capture";
            string audioInSourceId = "wasapi_input_capture";
            string audioEncoderId = "ffmpeg_aac";
            string videoSourceId = "game_capture";
#else
            string audioOutSourceId = "pulse_output_capture";
            string audioInSourceId = "pulse_input_capture";
            string audioEncoderId = "ffmpeg_aac";
            string videoSourceId = "xcomposite_input";
#endif
            // SETUP NEW AUDIO SOURCES & ENCODERS
            // - Create sources for output and input devices
            // TODO: isolate game audio and discord app audio
            // TODO: have user adjustable audio tracks, especially if the user is trying to use more than 6 tracks (6 is the limit)
            //       as of now, if the audio sources exceed 6 tracks, then those tracks will be defaulted to track 6 (index = 5)
            int totalDevices = 0;
            audioEncoders.TryAdd("combined", obs_audio_encoder_create(audioEncoderId, "combined", IntPtr.Zero, 0, IntPtr.Zero));
            obs_encoder_set_audio(audioEncoders["combined"], obs_get_audio());
            foreach (var (outputDevice, index) in SettingsService.Settings.captureSettings.outputDevices.WithIndex()) {
                audioSources.TryAdd("(output) " + outputDevice.deviceId, obs_audio_source_create(audioOutSourceId, "(output) " + outputDevice.deviceLabel, deviceId: outputDevice.deviceId));
                obs_set_output_source((uint)(index + 1), audioSources["(output) " + outputDevice.deviceId]);
                obs_source_set_audio_mixers(audioSources["(output) " + outputDevice.deviceId], 1 | (uint)(1 << Math.Min(index + 1, 5)));
                obs_source_set_volume(audioSources["(output) " + outputDevice.deviceId], outputDevice.deviceVolume / (float)100);
                if (index + 1 < 6) {
                    audioEncoders.TryAdd("(output) " + outputDevice.deviceId, obs_audio_encoder_create(audioEncoderId, "(output) " + outputDevice.deviceLabel, IntPtr.Zero, (UIntPtr)index + 1, IntPtr.Zero));
                    obs_encoder_set_audio(audioEncoders["(output) " + outputDevice.deviceId], obs_get_audio());
                }
                else
                    Logger.WriteLine($"[Warning] Exceeding 6 audio sources ({index + 1}), cannot add another track (max = 6)");
                totalDevices++;
            }
            foreach (var (inputDevice, index) in SettingsService.Settings.captureSettings.inputDevices.WithIndex()) {
                audioSources.TryAdd("(input) " + inputDevice.deviceId, obs_audio_source_create(audioInSourceId, "(input) " + inputDevice.deviceLabel, deviceId: inputDevice.deviceId, mono: true));
                obs_set_output_source((uint)(index + totalDevices + 1), audioSources["(input) " + inputDevice.deviceId]);
                obs_source_set_audio_mixers(audioSources["(input) " + inputDevice.deviceId], 1 | (uint)(1 << Math.Min(index + totalDevices + 1, 5)));
                obs_source_set_volume(audioSources["(input) " + inputDevice.deviceId], inputDevice.deviceVolume / (float)100);
                if (index + totalDevices + 1 < 6) {
                    audioEncoders.TryAdd("(input) " + inputDevice.deviceId, obs_audio_encoder_create(audioEncoderId, "(input) " + inputDevice.deviceLabel, IntPtr.Zero, (UIntPtr)(index + totalDevices + 1), IntPtr.Zero));
                    obs_encoder_set_audio(audioEncoders["(input) " + inputDevice.deviceId], obs_get_audio());
                }
                else
                    Logger.WriteLine($"[Warning] Exceeding 6 audio sources ({index + totalDevices + 1}), cannot add another track (max = 6)");

                if (inputDevice.denoiser) {
                    nint settings = obs_data_create();
                    obs_data_set_string(settings, "method", "denoiser");
                    obs_data_set_string(settings, "versioned_id", "noise_suppress_filter_v2");
                    nint noiseSuppressFilter = obs_source_create("noise_suppress_filter", "Noise Suppression", settings, IntPtr.Zero);
                    obs_source_filter_add(audioSources["(input) " + inputDevice.deviceId], noiseSuppressFilter);
                    obs_data_release(settings);
                }
            }

            string encoder = SettingsService.Settings.captureSettings.encoder;
            string rateControl = SettingsService.Settings.captureSettings.rateControl;
            string fileFormat = SettingsService.Settings.captureSettings.fileFormat.format;

            if (session.ForceDisplayCapture == false) {
                // SETUP NEW VIDEO SOURCE
                // - Create a source for the game_capture in channel 0
                IntPtr videoSourceSettings = obs_data_create();
                obs_data_set_string(videoSourceSettings, "capture_mode", WindowService.IsFullscreen(windowHandle) ? "any_fullscreen" : "window");
                obs_data_set_string(videoSourceSettings, "capture_window", windowClassNameId);
                obs_data_set_string(videoSourceSettings, "window", windowClassNameId);
                videoSources.TryAdd("gameplay", obs_source_create(videoSourceId, "gameplay", videoSourceSettings, IntPtr.Zero));
                obs_data_release(videoSourceSettings);

                // SETUP VIDEO ENCODER
                videoEncoders.TryAdd(encoder, GetVideoEncoder(encoder, rateControl, fileFormat));
                obs_encoder_set_video(videoEncoders[encoder], obs_get_video());
                obs_set_output_source(0, videoSources["gameplay"]);

                // attempt to wait for game_capture source to hook first
                if (videoSourceId == "game_capture") {
                    retryAttempt = 0;
                    Logger.WriteLine($"Waiting for successful graphics hook for [{windowClassNameId}]...");

                    // SETUP HOOK SIGNAL HANDLERS
                    signal_handler_connect(obs_output_get_signal_handler(videoSources["gameplay"]), "hooked", (data, cd) => {
                        Logger.WriteLine("hooked");
                    }, IntPtr.Zero);
                    signal_handler_connect(obs_output_get_signal_handler(videoSources["gameplay"]), "unhooked", (data, cd) => {
                        Logger.WriteLine("unhooked");
                    }, IntPtr.Zero);

                    while (signalGCHookSuccess == false && retryAttempt < Math.Min(maxRetryAttempts + signalGCHookAttempt, 30)) {
                        await Task.Delay(retryInterval);
                        retryAttempt++;
                    }
                }
            }
            else {
                videoEncoders.TryAdd(encoder, GetVideoEncoder(encoder, rateControl, fileFormat));
                obs_encoder_set_video(videoEncoders[encoder], obs_get_video());
            }
            signalGCHookAttempt = 0;

            if (videoSourceId == "game_capture" && signalGCHookSuccess == false) {
                if (session.ForceDisplayCapture == false) {
                    Logger.WriteLine($"Unable to get graphics hook for [{windowClassNameId}] after {retryAttempt} attempts");
                }

                Process process;

                try {
                    process = Process.GetProcessById(session.Pid);
                }
                catch {
                    ReleaseOutput();
                    ReleaseSources();
                    ReleaseEncoders();
                    return false;
                }

                //This is due to a bug in System.Diagnostics.Process (process.HasExited) Class https://www.giorgi.dev/net/access-denied-process-bugs/
                bool processHasExited = false;
                try {
                    processHasExited = process.HasExited;
                }
                catch (Exception ex) {
                    Logger.WriteLine("Could not get process exit status: " + ex.ToString());
                }

                if (SettingsService.Settings.captureSettings.useDisplayCapture && !processHasExited) {
                    Logger.WriteLine("Attempting to use display capture instead");
                    StartDisplayCapture();
                }
                else {
                    ReleaseOutput();
                    ReleaseSources();
                    ReleaseEncoders();
                    return false;
                }
            }
            retryAttempt = 0;

            // SETUP NEW OUTPUT
            if (SettingsService.Settings.captureSettings.useReplayBuffer) {
                IntPtr bufferOutputSettings = obs_data_create();
                obs_data_set_string(bufferOutputSettings, "directory", dir);
                obs_data_set_string(bufferOutputSettings, "format", "%CCYY-%MM-%DD %hh-%mm-%ss-ses");
                obs_data_set_string(bufferOutputSettings, "extension", fileFormat);
                obs_data_set_int(bufferOutputSettings, "max_time_sec", SettingsService.Settings.captureSettings.replayBufferDuration);
                obs_data_set_int(bufferOutputSettings, "max_size_mb", SettingsService.Settings.captureSettings.replayBufferSize);
                output = obs_output_create("replay_buffer", "replay_buffer_output", bufferOutputSettings, IntPtr.Zero);

                obs_data_release(bufferOutputSettings);
            }
            else {
                output = obs_output_create("ffmpeg_muxer", "simple_ffmpeg_output", IntPtr.Zero, IntPtr.Zero);


                // SETUP OUTPUT SETTINGS
                IntPtr outputSettings = obs_data_create();
                obs_data_set_string(outputSettings, "path", videoSavePath);
                obs_output_update(output, outputSettings);
                obs_data_release(outputSettings);
            }
            signal_handler_connect(obs_output_get_signal_handler(output), "stop", outputStopCb, IntPtr.Zero);

            obs_output_set_video_encoder(output, videoEncoders[encoder]);
            nuint idx = 0;
            foreach (var audioEncoder in audioEncoders) {
                obs_output_set_audio_encoder(output, audioEncoder.Value, idx);
                idx++;
            }

            // some quick checks on initializations before starting output
            bool canStartCapture = obs_output_can_begin_data_capture(output, 0);
            if (!canStartCapture) {
                while (!obs_output_initialize_encoders(output, 0) && retryAttempt < maxRetryAttempts) {
                    Logger.WriteLine($"Waiting for encoders to finish initializing... retry attempt #{retryAttempt}");
                    await Task.Delay(retryInterval);
                    retryAttempt++;
                }
                if (retryAttempt >= maxRetryAttempts) {
                    Logger.WriteLine("Unable to get encoders to initialize");
                    ReleaseOutput();
                    ReleaseSources();
                    ReleaseEncoders();
                    return false;
                }
            }
            retryAttempt = 0;

            // another null check just incase
            if (output == IntPtr.Zero) {
                Logger.WriteLine("LibObs output returned null, something really went wrong (this isn't suppose to happen)...");
                WebMessage.DisplayModal("An unexpected error occured. Detailed information written in logs.", "Recording Error", "warning");
                ReleaseOutput();
                ReleaseSources();
                ReleaseEncoders();
                return false;
            }

            // preparations complete, launch the rocket
            Logger.WriteLine($"LibObs output is starting [{bnum_allocs()}]...");
            bool outputStartSuccess = obs_output_start(output);
            if (outputStartSuccess != true) {
                string error = obs_output_get_last_error(output).Trim();
                Logger.WriteLine("LibObs output recording error: '" + error + "'");
                if (error.Length <= 0) {
                    WebMessage.DisplayModal("An unexpected error occured. Detailed information written in logs.", "Recording Error", "warning");
                }
                else {
                    WebMessage.DisplayModal(error, "Recording Error", "warning");
                }
                ReleaseOutput();
                ReleaseSources();
                ReleaseEncoders();
                return false;
            }
            else {
                Logger.WriteLine($"LibObs started recording [{session.Pid}] [{session.GameTitle}] [{windowClassNameId}]");
            }

            IntegrationService.Start(session.GameTitle);
            return true;
        }

        private void StartDisplayCapture() {
            ReleaseVideoSources();
            ResumeDisplayOutput();
            DisplayCapture = true;
        }

        private IntPtr GetVideoEncoder(string encoder, string rateControl, string format) {
            IntPtr videoEncoderSettings = obs_data_create();
            obs_data_set_bool(videoEncoderSettings, "use_bufsize", true);
            obs_data_set_string(videoEncoderSettings, "profile", "high");

            //Didn't really know how to handle the presets so it's just hacked for now.
            switch (encoder) {
                case "Hardware (NVENC HEVC)":
                case "Hardware (NVENC)":
                    obs_data_set_string(videoEncoderSettings, "preset", "Quality");
                    break;
                case "Software (x264)":
                    obs_data_set_string(videoEncoderSettings, "preset", "veryfast");
                    break;
                case "Hardware (FFMPEG VAAPI HEVC)":
                case "Hardware (FFMPEG VAAPI H.264)":
                    // TODO: properly implement vaapi device selection
                    // obs-ffmpeg-vaapi does not(?) automatically get GPU render device,
                    // so we have to try to do it. this current implementation only gets
                    // the first render device; does not account for other devices;
                    // does not account for hevc compatibility; etc.
                    // https://github.com/obsproject/obs-studio/blob/5697b085da46d6d50e129c264b9c08ecc37914fe/plugins/obs-ffmpeg/obs-ffmpeg-vaapi.c#L805
                    if (Directory.Exists("/dev/dri/by-path")) {
                        string[] pciDevices = Directory.GetFiles("/dev/dri/by-path/");
                        foreach (string fileName in pciDevices) {
                            if (fileName.EndsWith("-render", StringComparison.OrdinalIgnoreCase)) {
                                obs_data_set_string(videoEncoderSettings, "vaapi_device", fileName);
                                break;
                            }
                        }
                    }
                    obs_data_set_int(videoEncoderSettings, "profile", 100);
                    break;
            }

            obs_data_set_string(videoEncoderSettings, "rate_control", rate_controls[rateControl]);
            obs_data_set_int(videoEncoderSettings, "bitrate", (uint)SettingsService.Settings.captureSettings.bitRate * 1000);

            if (SettingsService.Settings.captureSettings.rateControl == "VBR") {
                obs_data_set_int(videoEncoderSettings, "max_bitrate", (uint)SettingsService.Settings.captureSettings.maxBitRate * 1000);
            }

            if (SettingsService.Settings.captureSettings.rateControl == "CQP") {
                obs_data_set_int(videoEncoderSettings, "cqp", (uint)SettingsService.Settings.captureSettings.cqLevel);
            }

            if (SettingsService.Settings.captureSettings.rateControl == "CRF") {
                obs_data_set_int(videoEncoderSettings, "crf", (uint)SettingsService.Settings.captureSettings.cqLevel);
            }

            // See https://github.com/obsproject/obs-studio/blob/9d2715fe72849bb8c1793bb964ba3d9dc2f189fe/UI/window-basic-main-outputs.cpp#L1310C1-L1310C1
            bool is_fragmented = format.StartsWith("fragmented", StringComparison.OrdinalIgnoreCase);
            bool is_lossless = rateControl == "Lossless";

            if (is_fragmented && !is_lossless) {
                string mux_frag = "movflags=frag_keyframe+empty_moov+delay_moov";
                obs_data_set_string(videoEncoderSettings, "muxer_settings", mux_frag);
                Logger.WriteLine("Video Encoder muxer flags: " + mux_frag);
            }

            IntPtr encoderPtr = obs_video_encoder_create(videoEncoderIds[encoder], "Replays Recorder", videoEncoderSettings, IntPtr.Zero);
            obs_data_release(videoEncoderSettings);
            return encoderPtr;
        }

        public override bool? TrySaveReplayBufferAndBookmarks() {
            if (IsUsingReplayBuffer()) {
                Logger.WriteLine("Trying to save replay buffer");
                calldata_t cd = new();
                var ph = obs_output_get_proc_handler(output);
                var res = proc_handler_call(ph, "save", cd);
                if (!res) {
                    Logger.WriteLine("Failed to save replay buffer");
                    return false;
                }

                calldata_t pathcd = new();
                var success = proc_handler_call(ph, "get_last_replay", pathcd);
                if (!success) {
                    Logger.WriteLine("Could not get replay save location");
                    return false;
                }

                if (!calldata_get_string(pathcd, "path", out string path)) {
                    Logger.WriteLine($"Could not get path of callback data (replay buffer): {path}");
                    return false;
                }

                var fileName = Path.GetFileName(path);

                Logger.WriteLine($"Successfully saved replay buffer to {path}");
                RecordingService.lastVideoDuration = GetVideoDuration(path);
                BookmarkService.ApplyBookmarkToSavedVideo("/" + fileName);

                StorageService.ManageStorage();


#if RELEASE && WINDOWS
                var t = Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy, true));
#else
                var t = Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy));
#endif

                t.ContinueWith(h => WebMessage.SendMessage(h.Result));
                return true;
            }

            return null;
        }

        public override void LostFocus() {
            //if (DisplayCapture) PauseDisplayOutput();
        }

        public override void GainedFocus() {
            //if (DisplayCapture) ResumeDisplayOutput();
        }

        public void PauseDisplayOutput() {
            ReleaseVideoSources();
        }

        public void ResumeDisplayOutput() {
            var monitor_id = "";
#if WINDOWS
            var screen = windowHandle == 0 ? System.Windows.Forms.Screen.PrimaryScreen : System.Windows.Forms.Screen.FromHandle(windowHandle);
            monitor_id = WindowService.GetMonitorId(screen.DeviceName);
#endif
            IntPtr videoSourceSettings = obs_data_create();
            // obs_data_set_int(videoSourceSettings, "method", 0); // automatic
            if (monitor_id != "") {
                obs_data_set_string(videoSourceSettings, "monitor_id", monitor_id);
            }
            videoSources.TryAdd("display", obs_source_create("monitor_capture", "display", videoSourceSettings, IntPtr.Zero));
            obs_data_release(videoSourceSettings);
            obs_set_output_source(0, videoSources["display"]);
        }

        public void GetAvailableEncoders() {
            UIntPtr idx = UIntPtr.Zero;
            string id = "";
            List<string> availableEncoders = new();
            while (obs_enum_encoder_types(idx, ref id)) {
                idx = UIntPtr.Add(idx, 1);
                if (id == string.Empty)
                    continue;
                switch (id) {
                    case "jim_av1_nvenc":
                        availableEncoders.Add("Hardware (NVENC AV1)");
                        break;
                    case "jim_nvenc":
                        availableEncoders.Add("Hardware (NVENC)");
                        break;
                    case "jim_hevc_nvenc":
                        availableEncoders.Add("Hardware (NVENC HEVC)");
                        break;
                    case "amd_amf_h264":
                        availableEncoders.Add("Hardware (AMF)");
                        break;
                    case "h264_texture_amf":
                        availableEncoders.Add("Hardware (AMD AVC)");
                        break;
                    case "h265_texture_amf":
                        availableEncoders.Add("Hardware (AMD HEVC)");
                        break;
                    case "av1_texture_amf":
                        availableEncoders.Add("Hardware (AMD AV1)");
                        break;
                    case "obs_qsv11":
                        availableEncoders.Add("Hardware (QSV)");
                        break;
                    case "ffmpeg_vaapi":
                        availableEncoders.Add("Hardware (FFMPEG VAAPI H.264)");
                        break;
                    case "hevc_ffmpeg_vaapi":
                        availableEncoders.Add("Hardware (FFMPEG VAAPI HEVC)");
                        break;
                    case "obs-va-vah264enc":
                        availableEncoders.Add("Hardware (GSTREAMER VAAPI H.264)");
                        break;
                }
            }
            //As x264 is a software encoder, it must be supported on all platforms
            availableEncoders.Add("Software (x264)");
            Logger.WriteLine("Encoder options: " + string.Join(", ", availableEncoders));
            SettingsService.Settings.captureSettings.encodersCache = availableEncoders;
            if (!availableEncoders.Contains(SettingsService.Settings.captureSettings.encoder)) {
                if (!string.IsNullOrWhiteSpace(SettingsService.Settings.captureSettings.encoder))
                    WebMessage.DisplayModal($"The previously selected encoder is no longer available. The encoder has been reset to the default option: {availableEncoders[0]}.", "Encoder warning", "warning");

                SettingsService.Settings.captureSettings.encoder = availableEncoders[0];
            }
            SettingsService.SaveSettings();
        }

        public bool HasNvidiaAudioSDK() {
            bool exists = Path.Exists("C:\\Program Files\\NVIDIA Corporation\\NVIDIA Audio Effects SDK");
            if (SettingsService.Settings.captureSettings.hasNvidiaAudioSDK != exists) {
                SettingsService.Settings.captureSettings.hasNvidiaAudioSDK = exists;
                SettingsService.SaveSettings();
            }
            return exists;
        }

        public void GetAvailableRateControls() {
            Logger.WriteLine("Selected encoder: " + SettingsService.Settings.captureSettings.encoder);
            if (videoEncoderLink.TryGetValue(SettingsService.Settings.captureSettings.encoder, out List<string> availableRateControls)) {
                Logger.WriteLine("Rate Control options: " + string.Join(", ", availableRateControls));
                SettingsService.Settings.captureSettings.rateControlCache = availableRateControls;
                if (!availableRateControls.Contains(SettingsService.Settings.captureSettings.rateControl))
                    SettingsService.Settings.captureSettings.rateControl = availableRateControls[0];
                SettingsService.SaveSettings();
            }
        }

        public void GetAvailableFileFormats() {
            Logger.WriteLine("File format: " + SettingsService.Settings.captureSettings.fileFormat);

            var selectedFormat = SettingsService.Settings.captureSettings.fileFormat;

            // Check if we have an invalid file format selected
            if (selectedFormat == null
                || file_formats.Where(x => x.format == selectedFormat.format).Any() == false) {
                // Invalid file format, default to file_format_default.
                selectedFormat = file_format_default;
                SettingsService.Settings.captureSettings.fileFormat = selectedFormat;
            }

            SettingsService.Settings.captureSettings.fileFormatsCache = file_formats;
            SettingsService.SaveSettings();
        }

        public override async Task<bool> StopRecording() {
            if (output == IntPtr.Zero || isStopping) return false;
            isStopping = true;
            signalGCHookSuccess = false;
            var session = RecordingService.GetCurrentSession();

            // Stop output
            obs_output_stop(output);
            // attempt to check if output signalled stop
            int retryAttempt = 0;
            while (signalOutputStop == false && retryAttempt < maxRetryAttempts / 2) {
                Logger.WriteLine($"Waiting for obs_output to stop... retry attempt #{retryAttempt}");
                await Task.Delay(retryInterval);
                retryAttempt++;
            }
            isStopping = false;
            if (retryAttempt >= maxRetryAttempts / 2) {
                Logger.WriteLine($"Failed to get obs_output_stop signal, forcing output to stop.");
                obs_output_force_stop(output);
            }

            bool isReplayBuffer = IsUsingReplayBuffer();

            // CLEANUP
            ReleaseOutput();
            ReleaseSources();
            ReleaseEncoders();

            DisplayCapture = false;

            if (!isReplayBuffer) {
                Logger.WriteLine($"Session recording saved to {videoSavePath}");
                RecordingService.lastVideoDuration = GetVideoDuration(videoSavePath);
            }

            if (IntegrationService.ActiveGameIntegration is LeagueOfLegendsIntegration lol) {
                GetOrCreateMetadata(videoSavePath);
                lol.UpdateMetadataWithStats(videoSavePath);
            }

#if RELEASE && WINDOWS
            var t = Task.Run(() => {
                var v = GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy, true);
                WebMessage.SendMessage(v);
                return Task.CompletedTask;
            });
#else
            var t = Task.Run(() => {
                var v = GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy);
                WebMessage.SendMessage(v);
                return Task.CompletedTask;
            });
#endif
            IntegrationService.Shutdown();
            if (!isReplayBuffer)
                BookmarkService.ApplyBookmarkToSavedVideo("/" + Path.GetFileName(videoSavePath));

            Logger.WriteLine($"LibObs stopped recording {session.Pid} {session.GameTitle} [{bnum_allocs()}]");
            return !signalOutputStop;
        }

        private static void RestartRecording() {
            if (output == IntPtr.Zero) return;
            RecordingService.RestartRecording();
        }

        private IntPtr obs_audio_source_create(string id, string name, IntPtr settings = new(), string deviceId = "default", bool mono = false) {
            if (settings == IntPtr.Zero) {
                settings = obs_data_create();
                obs_data_set_string(settings, "device_id", deviceId);
            }
            IntPtr source = obs_source_create(id, name, settings, IntPtr.Zero);

            // Down audio source mix to mono
            // TODO: make this a user configurable setting instead
            if (mono) {
                uint flags = obs_source_get_flags(source) | (1 << 1);
                obs_source_set_flags(source, flags);
            }
            obs_data_release(settings);
            return source;
        }

        public static void ResetAudio() {
            obs_audio_info avi = new() {
                samples_per_sec = 44100,
                speakers = speaker_layout.SPEAKERS_STEREO
            };
            bool resetAudioCode = obs_reset_audio(ref avi);
        }

        public static void ResetVideo(nint windowHandle = 0, int outputWidth = 1, int outputHeight = 1) {
            //Screen ratio to calculate output width.
            double screenRatio = (double)outputWidth / (double)outputHeight;

#if WINDOWS
            var screen = windowHandle == 0 ? System.Windows.Forms.Screen.PrimaryScreen : System.Windows.Forms.Screen.FromHandle(windowHandle);
            var screenWidth = screen.Bounds.Width;
            var screenHeight = screen.Bounds.Height;
#else
            var screenWidth = 1920;
            var screenHeight = 1080;
#endif

            obs_video_info ovi = new() {
                adapter = 0,
#if WINDOWS
                graphics_module = "libobs-d3d11",
#else
                graphics_module = "libobs-opengl",
#endif
                fps_num = (uint)SettingsService.Settings.captureSettings.frameRate,
                fps_den = 1,
                base_width = (uint)(outputWidth > 1 ? outputWidth : screenWidth),
                base_height = (uint)(outputHeight > 1 ? outputHeight : screenHeight),
                output_width = (uint)(outputWidth > 1 ? Convert.ToInt32(SettingsService.Settings.captureSettings.resolution * screenRatio) : screenWidth),
                output_height = (uint)(outputHeight > 1 ? SettingsService.Settings.captureSettings.resolution : screenHeight),
                output_format = video_format.VIDEO_FORMAT_NV12,
                gpu_conversion = true,
                colorspace = video_colorspace.VIDEO_CS_DEFAULT,
                range = video_range_type.VIDEO_RANGE_DEFAULT,
                scale_type = obs_scale_type.OBS_SCALE_BILINEAR
            };
            int resetVideoCode = obs_reset_video(ref ovi);
            if (resetVideoCode != 0) {
                throw new Exception("error on libobs reset video: " + ((VideoResetError)resetVideoCode).ToString());
            }
        }

        public void ReleaseSources() {
            ReleaseVideoSources();
            ReleaseAudioSources();
        }

        public void ReleaseVideoSources() {
            foreach (var videoSource in videoSources.Values) {
                obs_source_remove(videoSource);
                obs_source_release(videoSource);
            }
            videoSources.Clear();
            Logger.WriteLine("Released Video Sources.");
        }

        public void ReleaseAudioSources() {
            foreach (var audioSource in audioSources.Values) {
                obs_source_remove(audioSource);
                obs_source_release(audioSource);
            }
            audioSources.Clear();
            Logger.WriteLine("Released Audio Sources.");
        }

        public void ReleaseEncoders() {
            Logger.WriteLine("Releasing Video Encoders.");
            foreach (var encoder in videoEncoders) {
                var reference = obs_encoder_get_ref(encoder.Value);
                if (reference == IntPtr.Zero) {
                    Logger.WriteLine($"Could not release video encoder ({encoder.Key}), does not exist.");
                    continue;
                }
                obs_encoder_release(reference);
                obs_encoder_release(encoder.Value);
            }
            videoEncoders.Clear();
            Logger.WriteLine("Releasing Audio Encoders.");
            foreach (var encoder in audioEncoders) {
                var reference = obs_encoder_get_ref(encoder.Value);
                if (reference == IntPtr.Zero) {
                    Logger.WriteLine($"Could not release audio encoder ({encoder.Key}), does not exist.");
                    continue;
                }
                obs_encoder_release(reference);
                obs_encoder_release(encoder.Value);
            }
            audioEncoders.Clear();
            Logger.WriteLine("Released Encoders.");
        }

        public void ReleaseOutput() {
            Logger.WriteLine("Releasing Output.");
            var reference = obs_output_get_ref(output);
            if (reference == IntPtr.Zero) {
                output = IntPtr.Zero;
                Logger.WriteLine("Could not release output, does not exist.");
                return;
            }
            signal_handler_disconnect(obs_output_get_signal_handler(reference), "stop", outputStopCb, IntPtr.Zero);
            obs_output_release(reference);
            obs_output_release(output);
            output = IntPtr.Zero;
            Logger.WriteLine("Released Output.");
        }

        public bool IsUsingReplayBuffer() {
            var refOutput = obs_output_get_ref(output);
            if (refOutput == IntPtr.Zero) {
                Logger.WriteLine("Failed to get output reference");
                return false;
            }

            var id = obs_output_get_id(refOutput);
            return id == "replay_buffer";
        }
    }
}
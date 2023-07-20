using obs_net;
using RePlays.Integrations;
using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static obs_net.Obs;
using static RePlays.Utils.Functions;

namespace RePlays.Recorders {
    public class LibObsRecorder : BaseRecorder {
        public bool Connected { get; private set; }
        public bool DisplayCapture;
        public bool isStopping;
        static string videoSavePath = "";
        static string videoNameTimeStamp = "";
        static IntPtr windowHandle = IntPtr.Zero;
        static IntPtr output = IntPtr.Zero;
        static Rect windowSize;

        Dictionary<string, IntPtr> audioSources = new(), videoSources = new();
        Dictionary<string, IntPtr> audioEncoders = new(), videoEncoders = new();

        private Dictionary<string, string> encoder_ids = new() {
            {"Software (x264)", "obs_x264"},
            {"Hardware (NVENC)", "jim_nvenc"},
            {"Hardware (QSV)", "obs_qsv11"},
            {"Hardware (AMF)", "amd_amf_h264"}
        };

        private Dictionary<string, string> rate_controls = new() {
            {"VBR", "VBR"},
            {"CBR", "CBR"},
            {"CQP", "CQP"},
            {"Lossless", "Lossless"},
            {"ABR", "ABR"},
            {"CRF", "CRF"},
        };

        private Dictionary<string, List<string>> encoder_link = new() {
            { "Hardware (NVENC)", new List<string> { "VBR", "CBR", "CQP", "Lossless" } },
            { "Software (x264)", new List<string> { "VBR", "CBR", "CRF" } },
            { "Hardware (AMF)", new List<string> { "VBR", "CBR", "ABR", "CRF" } },
            { "Hardware (QSV)", new List<string> { "VBR", "CBR" } }
        };

        static bool signalOutputStop = false;
        static bool signalGCHookSuccess = false;
        static int signalGCHookAttempt = 0;

        static signal_callback_t outputStopCb;

        public override void Start() {
            if (Connected) return;

            // STARTUP
            if (obs_initialized()) {
                throw new Exception("error: obs already initialized");
            }

            // Warning: if you try to access methods/vars/etc. that are not static within the log handler,
            // it will cause a System.ExecutionEngineException, something to do with illegal memory
            base_set_log_handler(new log_handler_t((lvl, msg, args, p) => {
                try {
                    using (va_list arglist = new va_list(args)) {
                        object[] objs = arglist.GetObjectsByFormat(msg);
                        string formattedMsg = Printf.sprintf(msg, objs);

                        Logger.WriteLine(((LogErrorLevel)lvl).ToString() + ": " + formattedMsg);

                        // a very crude way to see if game_capture source has successfully hooked/capture application....
                        // does game_capture source provide any signals that we can alternatively use?
                        if (formattedMsg == "[game-capture: 'gameplay'] Starting capture") {
                            if (signalGCHookSuccess && RecordingService.IsRecording) {
                                // everytime the "Starting capture" signal occurs, there could be a possibility that the game window has resized
                                // check to see if windowSize is different from currentSize, if so, restart recording with correct output resolution
                                Rect currentSize = GetWindowSize(windowHandle);
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
                }
                catch (Exception e) {
                    // something went wrong, most likely an issue with our sprintf implementation?
                    Logger.WriteLine(e.ToString());
                    Logger.WriteLine(e.StackTrace);
                }
            }), IntPtr.Zero);

            Logger.WriteLine("libobs version: " + obs_get_version_string());
            if (!obs_startup("en-US", null, IntPtr.Zero)) {
                throw new Exception("error on libobs startup");
            }
            obs_add_data_path("./data/libobs/");
            obs_add_module_path("./obs-plugins/64bit/", "./data/obs-plugins/%module%/");
            obs_load_all_modules();
            obs_log_loaded_modules();

            ResetAudio();
            ResetVideo();

            obs_post_load_modules();

            // Warning: if you try to access methods/vars/etc. that are not static within the log handler,
            // it will cause a System.ExecutionEngineException, something to do with illegal memory
            outputStopCb = new signal_callback_t((data, cd) => {
                signalOutputStop = true;
            });

            Connected = true;
            Logger.WriteLine("Successfully started LibObs!");
        }

        const int retryInterval = 2000; // 2 second
        const int maxRetryAttempts = 20; // 30 retries
        public override async Task<bool> StartRecording() {
            if (output != IntPtr.Zero) return false;

            signalOutputStop = false;
            int retryAttempt = 0;
            var session = RecordingService.GetCurrentSession();

            // If session is empty, this is a manual record attempt. Lets try to yolo record the foregroundwindow
            if (session.Pid == 0 && GetForegroundProcess(out int processId, out nint hwid)) {
                if (processId != 0 || hwid != 0) {
                    DetectionService.GetExecutablePathFromProcessId((uint)processId, out string executablePath);
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
                windowHandle = GetWindowHandleByProcessId(session.Pid, retryAttempt % 2 == 1);
            }
            if (retryAttempt >= maxRetryAttempts) {
                return false;
            }
            retryAttempt = 0;

            string dir = Path.Join(GetPlaysFolder(), "/" + MakeValidFolderNameSimple(session.GameTitle) + "/");
            Directory.CreateDirectory(dir);
            videoNameTimeStamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            videoSavePath = Path.Join(dir, videoNameTimeStamp + "-ses.mp4");

            // Get the window class name
            var windowClassNameId = GetWindowTitle(windowHandle) + ":" + GetClassName(windowHandle) + ":" + Path.GetFileName(session.Exe);

            // get game's window size and change output to match
            windowSize = GetWindowSize(windowHandle);
            // sometimes, the inital window size might be in a middle of a transition, and gives us a weird dimension
            // this is a quick a dirty check: if there aren't more than 1120 pixels, we can assume it needs a retry
            while (windowSize.GetWidth() + windowSize.GetHeight() < 1120 && retryAttempt < maxRetryAttempts) {
                Logger.WriteLine($"Waiting to retrieve correct window size (currently {windowSize.GetWidth()}x{windowSize.GetHeight()})... retry attempt #{retryAttempt}");
                await Task.Delay(retryInterval);
                retryAttempt++;
                windowSize = GetWindowSize(windowHandle);
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

            // SETUP NEW AUDIO SOURCES & ENCODERS
            // - Create sources for output and input devices
            // TODO: isolate game audio and discord app audio
            // TODO: have user adjustable audio tracks, especially if the user is trying to use more than 6 tracks (6 is the limit)
            //       as of now, if the audio sources exceed 6 tracks, then those tracks will be defaulted to track 6 (index = 5)
            int totalDevices = 0;
            audioEncoders.TryAdd("combined", obs_audio_encoder_create("ffmpeg_aac", "combined", IntPtr.Zero, 0, IntPtr.Zero));
            obs_encoder_set_audio(audioEncoders["combined"], obs_get_audio());
            foreach (var (outputDevice, index) in SettingsService.Settings.captureSettings.outputDevices.WithIndex()) {
                audioSources.TryAdd("(output) " + outputDevice.deviceId, obs_audio_source_create("wasapi_output_capture", "(output) " + outputDevice.deviceLabel, deviceId: outputDevice.deviceId));
                obs_set_output_source((uint)(index + 1), audioSources["(output) " + outputDevice.deviceId]);
                obs_source_set_audio_mixers(audioSources["(output) " + outputDevice.deviceId], 1 | (uint)(1 << Math.Min(index + 1, 5)));
                obs_source_set_volume(audioSources["(output) " + outputDevice.deviceId], outputDevice.deviceVolume / (float)100);
                if (index + 1 < 6) {
                    audioEncoders.TryAdd("(output) " + outputDevice.deviceId, obs_audio_encoder_create("ffmpeg_aac", "(output) " + outputDevice.deviceLabel, IntPtr.Zero, (UIntPtr)index + 1, IntPtr.Zero));
                    obs_encoder_set_audio(audioEncoders["(output) " + outputDevice.deviceId], obs_get_audio());
                }
                else
                    Logger.WriteLine($"[Warning] Exceeding 6 audio sources ({index + 1}), cannot add another track (max = 6)");
                totalDevices++;
            }
            foreach (var (inputDevice, index) in SettingsService.Settings.captureSettings.inputDevices.WithIndex()) {
                audioSources.TryAdd("(input) " + inputDevice.deviceId, obs_audio_source_create("wasapi_input_capture", "(input) " + inputDevice.deviceLabel, deviceId: inputDevice.deviceId, mono: true));
                obs_set_output_source((uint)(index + totalDevices + 1), audioSources["(input) " + inputDevice.deviceId]);
                obs_source_set_audio_mixers(audioSources["(input) " + inputDevice.deviceId], 1 | (uint)(1 << Math.Min(index + totalDevices + 1, 5)));
                obs_source_set_volume(audioSources["(input) " + inputDevice.deviceId], inputDevice.deviceVolume / (float)100);
                if (index + totalDevices + 1 < 6) {
                    audioEncoders.TryAdd("(input) " + inputDevice.deviceId, obs_audio_encoder_create("ffmpeg_aac", "(input) " + inputDevice.deviceLabel, IntPtr.Zero, (UIntPtr)(index + totalDevices + 1), IntPtr.Zero));
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

            // SETUP NEW VIDEO SOURCE
            // - Create a source for the game_capture in channel 0
            IntPtr videoSourceSettings = obs_data_create();
            obs_data_set_string(videoSourceSettings, "capture_mode", IsFullscreen(windowHandle) ? "any_fullscreen" : "window");
            obs_data_set_string(videoSourceSettings, "window", windowClassNameId);
            videoSources.TryAdd("gameplay", obs_source_create("game_capture", "gameplay", videoSourceSettings, IntPtr.Zero));
            obs_data_release(videoSourceSettings);

            // SETUP VIDEO ENCODER
            string encoder = SettingsService.Settings.captureSettings.encoder;
            string rateControl = SettingsService.Settings.captureSettings.rateControl;
            videoEncoders.TryAdd(encoder, GetVideoEncoder(encoder, rateControl));
            obs_encoder_set_video(videoEncoders[encoder], obs_get_video());
            obs_set_output_source(0, videoSources["gameplay"]);

            // attempt to wait for game_capture source to hook first
            retryAttempt = 0;
            Logger.WriteLine($"Waiting for successful graphics hook for [{windowClassNameId}]...");
            while (signalGCHookSuccess == false && retryAttempt < Math.Min(maxRetryAttempts + signalGCHookAttempt, 30)) {
                await Task.Delay(retryInterval);
                retryAttempt++;
            }
            signalGCHookAttempt = 0;

            if (signalGCHookSuccess == false) {
                Logger.WriteLine($"Unable to get graphics hook for [{windowClassNameId}] after {retryAttempt} attempts");

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
            output = obs_output_create("ffmpeg_muxer", "simple_ffmpeg_output", IntPtr.Zero, IntPtr.Zero);
            signal_handler_connect(obs_output_get_signal_handler(output), "stop", outputStopCb, IntPtr.Zero);

            // SETUP OUTPUT SETTINGS
            IntPtr outputSettings = obs_data_create();
            obs_data_set_string(outputSettings, "path", videoSavePath);
            obs_output_update(output, outputSettings);
            obs_data_release(outputSettings);
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
                ReleaseOutput();
                ReleaseSources();
                ReleaseEncoders();
                return false;
            }

            // preparations complete, launch the rocket
            Logger.WriteLine($"LibObs output is starting [{bnum_allocs()}]...");
            bool outputStartSuccess = obs_output_start(output);
            if (outputStartSuccess != true) {
                Logger.WriteLine("LibObs output recording error: '" + obs_output_get_last_error(output) + "'");
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

        private IntPtr GetVideoEncoder(string encoder, string rateControl) {
            IntPtr videoEncoderSettings = obs_data_create();
            obs_data_set_bool(videoEncoderSettings, "use_bufsize", true);
            obs_data_set_string(videoEncoderSettings, "profile", "high");
            //Didn't really know how to handle the presets so it's just hacked for now.
            switch (encoder) {
                case "Hardware (NVENC)":
                    obs_data_set_string(videoEncoderSettings, "preset", "Quality");
                    break;
                case "Software (x264)":
                    obs_data_set_string(videoEncoderSettings, "preset", "veryfast");
                    break;
            }
            obs_data_set_string(videoEncoderSettings, "rate_control", rate_controls[rateControl]);
            obs_data_set_int(videoEncoderSettings, "bitrate", (uint)SettingsService.Settings.captureSettings.bitRate * 1000);
            if (SettingsService.Settings.captureSettings.rateControl == "VBR") {
                obs_data_set_int(videoEncoderSettings, "max_bitrate", (uint)SettingsService.Settings.captureSettings.bitRate * 1000);
            }
            IntPtr encoderPtr = obs_video_encoder_create(encoder_ids[encoder], "Replays Recorder", videoEncoderSettings, IntPtr.Zero);
            obs_data_release(videoEncoderSettings);
            return encoderPtr;
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
            IntPtr videoSourceSettings = obs_data_create();
            // obs_data_set_int(videoSourceSettings, "method", 0); // automatic
            // obs_data_set_string(videoSourceSettings, "monitor_id", @"\\\\?\\DISPLAY#DELA024#5&d5f75a2&0&UID37124#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}"); // don't set for primary monitor
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
                    case "jim_nvenc":
                        availableEncoders.Add("Hardware (NVENC)");
                        break;
                    case "amd_amf_h264":
                        availableEncoders.Add("Hardware (AMF)");
                        break;
                    case "obs_qsv11":
                        availableEncoders.Add("Hardware (QSV)");
                        break;
                }
            }
            //As x264 is a software encoder, it must be supported on all platforms
            availableEncoders.Add("Software (x264)");
            SettingsService.Settings.captureSettings.encodersCache = availableEncoders;
            if (!availableEncoders.Contains(SettingsService.Settings.captureSettings.encoder))
                SettingsService.Settings.captureSettings.encoder = availableEncoders[0];
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
            Logger.WriteLine("Encoder: " + SettingsService.Settings.captureSettings.encoder);
            if (encoder_link.TryGetValue(SettingsService.Settings.captureSettings.encoder, out List<string> availableRateControls)) {
                Logger.WriteLine("Rate Control options: " + string.Join(",", availableRateControls));
                SettingsService.Settings.captureSettings.rateControlCache = availableRateControls;
                if (!availableRateControls.Contains(SettingsService.Settings.captureSettings.rateControl))
                    SettingsService.Settings.captureSettings.rateControl = availableRateControls[0];
                SettingsService.SaveSettings();
            }
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
            while (signalOutputStop == false && retryAttempt < maxRetryAttempts) {
                Logger.WriteLine($"Waiting for obs_output to stop... retry attempt #{retryAttempt}");
                await Task.Delay(retryInterval);
                retryAttempt++;
            }
            isStopping = false;
            if (retryAttempt >= maxRetryAttempts) {
                return false;
            }

            // CLEANUP
            ReleaseOutput();
            ReleaseSources();
            ReleaseEncoders();

            Logger.WriteLine($"Session recording saved to {videoSavePath}");
            Logger.WriteLine($"LibObs stopped recording {session.Pid} {session.GameTitle} [{bnum_allocs()}]");
            DisplayCapture = false;
            RecordingService.lastVideoDuration = GetVideoDuration(videoSavePath);

            if (IntegrationService.ActiveGameIntegration is LeagueOfLegendsIntegration) {
                GetOrCreateMetadata(videoSavePath);
                UpdateMetadataWithStats(videoSavePath, LeagueOfLegendsIntegration.stats);
            }

            try {
                var t = await Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy));
                WebMessage.SendMessage(t);
            }
            catch (Exception e) {
                Logger.WriteLine(e.Message);
            }
            IntegrationService.Shutdown();
            BookmarkService.ApplyBookmarkToSavedVideo("/" + videoNameTimeStamp + "-ses.mp4");

            return true;
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
            var screenWidth = -1;
            var screenHeight = -1;
#endif

            obs_video_info ovi = new() {
                adapter = 0,
                graphics_module = "libobs-d3d11",
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
        }

        public void ReleaseAudioSources() {
            foreach (var audioSource in audioSources.Values) {
                obs_source_remove(audioSource);
                obs_source_release(audioSource);
            }
            audioSources.Clear();
        }

        public void ReleaseEncoders() {
            foreach (var videoEncoder in videoEncoders.Values) {
                obs_encoder_release(videoEncoder);
            }
            videoEncoders.Clear();
            foreach (var audioEncoder in audioEncoders.Values) {
                obs_encoder_release(audioEncoder);
            }
            audioEncoders.Clear();
        }

        public void ReleaseOutput() {
            signal_handler_disconnect(obs_output_get_signal_handler(output), "stop", outputStopCb, IntPtr.Zero);
            obs_output_release(output);
            output = IntPtr.Zero;
        }
    }
}

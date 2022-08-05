using System;
using System.IO;
using obs_net;
using static obs_net.Obs;
using RePlays.Services;
using RePlays.Utils;
using static RePlays.Utils.Functions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;

namespace RePlays.Recorders {
    public class LibObsRecorder : BaseRecorder {
        public bool Connected { get; private set; }

        static string videoSavePath = "";

        static IntPtr windowHandle;
        static IntPtr output;

        Dictionary<string, IntPtr> audioSources = new(), videoSources = new();
        Dictionary<string, IntPtr> audioEncoders = new(), videoEncoders = new();

        static bool signalOutputStop = false;
        static bool signalGCHookSuccess = false;

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
                            if(signalGCHookSuccess != false) {
                                // everytime the "Starting capture" signal occurs, there could be a possibility that the game window has resized
                                // if it has resized, restart output with correct size
                                RestartOutput();
                            }
                            signalGCHookSuccess = true;
                        }
                        else if (formattedMsg == "[game-capture: 'gameplay'] capture stopped") {
                            signalGCHookSuccess = false;
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

            // SETUP NEW OUTPUT
            output = obs_output_create("ffmpeg_muxer", "simple_ffmpeg_output", IntPtr.Zero, IntPtr.Zero);

            // Warning: if you try to access methods/vars/etc. that are not static within the log handler,
            // it will cause a System.ExecutionEngineException, something to do with illegal memory
            signal_handler_connect(obs_output_get_signal_handler(output), "stop", new signal_callback_t((data, cd) => {
                signalOutputStop = true;
            }), IntPtr.Zero);

            base.Start();

            Connected = true;
            Logger.WriteLine("Successfully started LibObs!");
        }

        const int retryInterval = 2000; // 2 second
        const int maxRetryAttempts = 5; // 10 seconds
        public override async Task<bool> StartRecording() {
            signalOutputStop = false;
            int retryAttempt = 0;
            var session = RecordingService.GetCurrentSession();

            // If session is empty, this is a manual record attempt. Lets try to yolo record the foregroundwindow
            if (session.Pid == 0) {
                int processId = GetForegroundProcessId();
                // if processId is 0, there was something wrong retrieving foreground process (this shouldn't normally happen)
                if (processId == 0)
                    return false;
                AutoDetectGame(processId, autoRecord:false);
                session = RecordingService.GetCurrentSession();
            }

            // attempt to retrieve process's window handle to retrieve class name and window title
            windowHandle = GetWindowHandleByProcessId(session.Pid);
            while (windowHandle == IntPtr.Zero && retryAttempt < maxRetryAttempts) {
                await Task.Delay(retryInterval);
                windowHandle = GetWindowHandleByProcessId(session.Pid);
                retryAttempt++;
                Logger.WriteLine(string.Format("Waiting to retrieve process handle... retry attempt #{0}", retryAttempt));
            }
            if (retryAttempt >= maxRetryAttempts) {
                return false;
            }
            retryAttempt = 0;

            string dir = Path.Join(GetPlaysFolder(), "/" + MakeValidFolderNameSimple(session.GameTitle) + "/");
            Directory.CreateDirectory(dir);
            videoSavePath = Path.Join(dir, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "-ses.mp4");

            // Get the window class name
            var windowClassNameId = GetWindowTitle(windowHandle) + ":" + GetClassName(windowHandle) + ":" + Path.GetFileName(session.Exe);

            // SETUP OUTPUT SETTINGS
            IntPtr outputSettings = obs_data_create();
            obs_data_set_string(outputSettings, "path", videoSavePath);
            obs_output_update(output, outputSettings);
            obs_data_release(outputSettings);

            // SETUP NEW AUDIO SOURCES
            // - Create a source for the desktop in channel 0, and the microphone in 1
            audioSources.TryAdd("desktop", obs_audio_source_create("wasapi_output_capture", "desktop")); // captures whole desktop
            obs_set_output_source(0, audioSources["desktop"]);
            obs_source_set_audio_mixers(audioSources["desktop"], 1 | (1 << 0));
            audioSources.TryAdd("microphone", obs_audio_source_create("wasapi_input_capture", "microphone"));
            obs_set_output_source(1, audioSources["microphone"]);
            obs_source_set_audio_mixers(audioSources["microphone"], 1 | (1 << 1));

            // SETUP AUDIO ENCODERS
            // - Each audio source needs an audio encoder, IF we plan to separate audio tracks in the future
            audioEncoders.TryAdd("aac0", obs_audio_encoder_create("ffmpeg_aac", "aac0", IntPtr.Zero, (UIntPtr)0, IntPtr.Zero));
            obs_output_set_audio_encoder(output, audioEncoders["aac0"], (UIntPtr)0);
            obs_encoder_set_audio(audioEncoders["aac0"], obs_output_audio(output));
            //audioEncoders.TryAdd("aac1", obs_audio_encoder_create("ffmpeg_aac", "aac1", IntPtr.Zero, (UIntPtr)1, IntPtr.Zero));
            //obs_output_set_audio_encoder(output, audioEncoders["aac1"], (UIntPtr)1);
            //obs_encoder_set_audio(audioEncoders["aac1"], obs_output_audio(output));

            // SETUP NEW VIDEO SOURCE
            // - Create a source for the game_capture in channel 2
            IntPtr videoSourceSettings = obs_data_create();
            obs_data_set_string(videoSourceSettings, "capture_mode", "window");
            obs_data_set_string(videoSourceSettings, "window", windowClassNameId);
            videoSources.TryAdd("gameplay", obs_source_create("game_capture", "gameplay", videoSourceSettings, IntPtr.Zero));
            obs_set_output_source(2, videoSources["gameplay"]);
            obs_data_release(videoSourceSettings);

            // SETUP VIDEO ENCODER
            IntPtr videoEncoderSettings = obs_data_create();
            obs_data_set_bool(videoEncoderSettings, "use_bufsize", true);
            obs_data_set_string(videoEncoderSettings, "profile", "high");
            obs_data_set_string(videoEncoderSettings, "preset", "veryfast");
            obs_data_set_string(videoEncoderSettings, "rate_control", "CRF");
            obs_data_set_int(videoEncoderSettings, "crf", 20);
            IntPtr videoEncoder = obs_video_encoder_create("obs_x264", "simple_h264_recording", videoEncoderSettings, IntPtr.Zero);
            obs_data_release(videoEncoderSettings);
            obs_encoder_set_video(videoEncoder, obs_get_video());
            obs_output_set_video_encoder(output, videoEncoder);

            // attempt to wait for game_capture source to hook first
            // this might take longer, so multiply maxRetryAttempts
            while (signalGCHookSuccess == false && retryAttempt < maxRetryAttempts * 3) {
                await Task.Delay(retryInterval);
                retryAttempt++;
                Logger.WriteLine(string.Format("Waiting for successful graphics hook for [{0}]... retry attempt #{1}", windowClassNameId, retryAttempt));
            }
            if (retryAttempt >= maxRetryAttempts * 3) {
                ReleaseSources();
                ReleaseEncoders();
                return false;
            }

            // get game's window size and change output to match
            Rect windowSize = GetWindowSize(windowHandle);
            Logger.WriteLine(String.Format("Game capture window size: {0}x{1}", windowSize.GetWidth(), windowSize.GetHeight()));
            ResetVideo(windowSize.GetWidth(), windowSize.GetHeight());

            // preparations complete, launch the rocket
            bool outputStartSuccess = obs_output_start(output);
            if (outputStartSuccess != true) {
                Logger.WriteLine("LibObs output recording error: '" + obs_output_get_last_error(output) + "'");
            } else {
                Logger.WriteLine(string.Format("LibObs started recording [{0}] [{1}] [{2}]", session.Pid, session.GameTitle, windowClassNameId));
            }

            return true;
        }

        public override async Task<bool> StopRecording() {
            signalGCHookSuccess = false;
            var session = RecordingService.GetCurrentSession();

            // Stop output
            obs_output_stop(output);
            // attempt to check if output signalled stop
            int retryAttempt = 0;
            while (signalOutputStop == false && retryAttempt < maxRetryAttempts) {
                await Task.Delay(retryInterval);
                retryAttempt++;
                Logger.WriteLine(string.Format("Waiting for obs_output to stop... retry attempt #{0}", retryAttempt));
            }
            if (retryAttempt >= maxRetryAttempts) {
                return false;
            }
            retryAttempt = 0;

            // CLEANUP
            ReleaseSources();
            ReleaseEncoders();

            Logger.WriteLine(string.Format("Session recording saved to {0}", videoSavePath));
            Logger.WriteLine(string.Format("LibObs stopped recording {0} {1}", session.Pid, session.GameTitle));

            try {
                var t = await Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy));
                WebMessage.SendMessage(t);
            }
            catch (Exception e) {
                Logger.WriteLine(e.Message);
            }

            return true;
        }

        public static async void RestartOutput() {
            // Stop output
            obs_output_stop(output);
            // attempt to check if output signalled stop
            int retryAttempt = 0;
            while (signalOutputStop == false && retryAttempt < maxRetryAttempts) {
                await Task.Delay(retryInterval);
                retryAttempt++;
                Logger.WriteLine(string.Format("Waiting for obs_output to stop... retry attempt #{0}", retryAttempt));
            }
            if (retryAttempt >= maxRetryAttempts) {
                Logger.WriteLine("Issue trying to stop output, giving up.");
                return;
            }

            videoSavePath = Path.Join(Path.GetDirectoryName(videoSavePath), DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "-ses.mp4");

            // SETUP OUTPUT SETTINGS
            IntPtr outputSettings = obs_data_create();
            obs_data_set_string(outputSettings, "path", videoSavePath);
            obs_output_update(output, outputSettings);
            obs_data_release(outputSettings);

            // get game's window size and change output to match
            Rect windowSize = GetWindowSize(windowHandle);
            Logger.WriteLine(String.Format("Game capture window size: {0}x{1}", windowSize.GetWidth(), windowSize.GetHeight()));
            ResetVideo(windowSize.GetWidth(), windowSize.GetHeight());

            // preparations complete, launch the rocket
            bool outputStartSuccess = obs_output_start(output);
            if (outputStartSuccess != true) {
                Logger.WriteLine("LibObs output recording error: '" + obs_output_get_last_error(output) + "'");
            }
            else {
                Logger.WriteLine("Force Restart Output successful.");
                signalOutputStop = false;
            }
        }

        public IntPtr obs_audio_source_create(string id, string name, IntPtr settings = new(), string deviceId = "default") {
            if (settings == IntPtr.Zero) {
                settings = obs_data_create();
                obs_data_set_string(settings, "device_id", deviceId);
            }
            IntPtr source = obs_source_create(id, name, settings, IntPtr.Zero);
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

        public static void ResetVideo(int outputWidth = 0, int outputHeight = 0) {
            obs_video_info ovi = new() {
                adapter = 0,
                graphics_module = "libobs-d3d11",
                fps_num = 60,
                fps_den = 1,
                base_width = (uint)(outputWidth > 1 ? outputWidth : System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width),
                base_height = (uint)(outputHeight > 1 ? outputHeight : System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height),
                output_width = (uint)(outputWidth > 1 ? outputWidth : System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width),
                output_height = (uint)(outputHeight > 1 ? outputHeight : System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height),
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
            foreach (var videoSource in videoSources.Values) {
                obs_source_remove(videoSource);
                obs_source_release(videoSource);
            }
            videoSources.Clear();
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
    }
}

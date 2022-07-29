using System.Management;
using System.Diagnostics;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using obs_net;
using static obs_net.Obs;
using RePlays.Services;
using RePlays.Utils;
using static RePlays.Utils.Functions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace RePlays.Recorders {
    public class LibObsRecorder : BaseRecorder {
        public bool Connected { get; private set; }

        ManagementEventWatcher pCreationWatcher = new(new EventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));
        ManagementEventWatcher pDeletionWatcher = new(new EventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance isa \"Win32_Process\""));

        string videoSavePath = "";

        // https://stackoverflow.com/a/14407610/8805016
        WinEventDelegate dele = null;

        IntPtr output;
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

            base_set_log_handler(new log_handler_t((lvl, msg, args, p) => {
                using (va_list arglist = new va_list(args)) {
                    object[] objs = arglist.GetObjectsByFormat(msg);
                    string formattedMsg = Printf.sprintf(msg, objs);

                    Logger.WriteLine(((LogErrorLevel)lvl).ToString() + ": " + formattedMsg);

                    // a very crude way to see if game_capture source has successfully hooked/capture application....
                    // does game_capture source provide any signals that we can alternatively use?
                    if(formattedMsg == "[game-capture: 'gameplay'] Starting capture") {
                        signalGCHookSuccess = true;
                    } else if (formattedMsg == "[game-capture: 'gameplay'] capture stopped") {
                        signalGCHookSuccess = false;
                    }
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

            obs_audio_info avi = new() {
                samples_per_sec = 44100,
                speakers = speaker_layout.SPEAKERS_STEREO
            };
            bool resetAudioCode = obs_reset_audio(ref avi);

            // Should match monitor resolution
            int baseWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            int baseHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            // output resolution
            int outputWidth = 1920;
            int outputHeight = 1080;


            obs_video_info ovi = new() {
                adapter = 0,
                graphics_module = "libobs-d3d11",
                fps_num = 60,
                fps_den = 1,
                base_width = (uint)baseWidth,
                base_height = (uint)baseHeight,
                output_width = (uint)outputWidth,
                output_height = (uint)outputHeight,
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

            obs_post_load_modules();

            // SETUP NEW OUTPUT
            output = obs_output_create("ffmpeg_muxer", "simple_ffmpeg_output", IntPtr.Zero, IntPtr.Zero);
            signal_handler_connect(obs_output_get_signal_handler(output), "stop", new signal_callback_t((data, cd) => {
                signalOutputStop = true; // this has to be static or else it will throw an engine fail exception. something to do with illegal memory
            }), IntPtr.Zero);

            pCreationWatcher.EventArrived += ProcessCreation_EventArrived;
            pDeletionWatcher.EventArrived += ProcessDeletion_EventArrived;
            pCreationWatcher.Start();
            pDeletionWatcher.Start();

            dele = new WinEventDelegate(WinEventProc);
            IntPtr m_hhook = SetWinEventHook(3, 3, IntPtr.Zero, dele, 0, 0, 0);

            Connected = true;
            Logger.WriteLine("Successfully started LibObs!");
        }

        public override void Stop() {
            throw new System.NotImplementedException();
        }

        const int retryInterval = 2000; // 2 second
        const int maxRetryAttempts = 5; // 10 seconds
        public override async Task<bool> StartRecording() {
            signalOutputStop = false;
            var session = RecordingService.GetCurrentSession();
            IntPtr handle;
            if (session.Pid == 0) {
                // If session is empty, this is a manual record attempt.  Lets try to yolo record the foregroundwindow
                handle = GetForegroundWindow();
                if (handle == IntPtr.Zero)
                    Logger.WriteLine(string.Format(""));
                if (GetWindowThreadProcessId(handle, out int processId) == 0)
                    Logger.WriteLine(string.Format(""));
                // string title = GetWindowTitle(handle);
                AutoDetectGame(processId, autoRecord:false);
                session = RecordingService.GetCurrentSession();
            }

            // attempt to retrieve process's window handle to retrieve class name and window title
            handle = EnumerateProcessWindowHandles(session.Pid).First();
            int retryAttempt = 0;
            while (handle == IntPtr.Zero && retryAttempt < maxRetryAttempts) {
                await Task.Delay(retryInterval);
                handle = EnumerateProcessWindowHandles(session.Pid).First();
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
            StringBuilder className = new(256);
            _ = GetClassName(handle, className, className.Capacity);
            var windowClassNameId = GetWindowTitle(handle) + ":" + className.ToString() + ":" + Path.GetFileName(session.Exe);

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

            // preparations complete, launch the rocket
            bool outputStartSuccess = obs_output_start(output);
            if (outputStartSuccess != true) {
                Console.WriteLine("LibObs output recording error: '" + obs_output_get_last_error(output) + "'");
            } else {
                Logger.WriteLine(string.Format("LibObs started recording [{0}] [{1}] [{2}]", session.Pid, session.GameTitle, windowClassNameId));
            }

            return true;
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

        public void ProcessCreation_EventArrived(object sender, EventArrivedEventArgs e) {
            if (RecordingService.IsRecording) return;

            try {
                if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription) {
                    int processId = Int32.Parse(instanceDescription.GetPropertyValue("Handle").ToString());
                    var executablePath = instanceDescription.GetPropertyValue("ExecutablePath");
                    var cmdLine = instanceDescription.GetPropertyValue("CommandLine"); // may or may not be useful in the future

                    AutoDetectGame(processId);
                }
            }
            catch (ManagementException) { }

            e.NewEvent.Dispose();
        }

        public void ProcessDeletion_EventArrived(object sender, EventArrivedEventArgs e) {
            if (!RecordingService.IsRecording) return;

            try {
                if (e.NewEvent.GetPropertyValue("TargetInstance") is ManagementBaseObject instanceDescription) {
                    int processId = Int32.Parse(instanceDescription.GetPropertyValue("Handle").ToString());

                    if (processId != 0) {
                        if (RecordingService.GetCurrentSession().Pid == processId)
                            RecordingService.StopRecording();
                    }
                }
            }
            catch (ManagementException) { }

            e.NewEvent.Dispose();
        }

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        // http://www.pinvoke.net/default.aspx/user32.getclassname
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        string GetWindowTitle(IntPtr hWnd) {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);

            if (GetWindowText(hWnd, Buff, nChars) == 0)
                return "";

            return Buff.ToString();
        }

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        // https://stackoverflow.com/a/67066227/8805016
        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn,
            IntPtr lParam);

        static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId) {
            var handles = new List<IntPtr>();

            try {
                foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                    EnumThreadWindows(thread.Id,
                        (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
            }
            catch (Exception) { }

            if (handles.Count == 0)
                handles.Add(IntPtr.Zero);

            return handles;
        }

        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            if (RecordingService.IsRecording) return;

            IntPtr handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
                return;
            if (GetWindowThreadProcessId(handle, out int processId) == 0)
                return;

            //string title = GetWindowTitle(handle);

            AutoDetectGame(processId);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, Int32 dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, uint nSize);

        /// <summary>
        /// <para>Checks to see if the process:</para>
        /// <para>1. contains in the game detection list (whitelist)</para>
        /// <para>2. does NOT contain in nongame detection list (blacklist)</para>
        /// <para>3. contains any graphics dll modules (directx, opengl)</para>
        /// <para>If 2 and 3 are true, we will also assume it is a "game"</para>
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="executablePath">Full path to executable, if possible</param>
        public void AutoDetectGame(int processId, string executablePath = null, bool autoRecord = true) {
            bool isGame = false, isNonGame = false;
            string exeFile = executablePath;
            string modules = "";

            Process[] processlist = Process.GetProcesses();
            using Process process = processlist.FirstOrDefault(pr => pr.Id == processId);

            if (process != null) {
                if (exeFile == null) {
                    exeFile = process.ProcessName + ".exe";
                }

                IntPtr processHandle = OpenProcess(0x0400 | 0x0010, //PROCESS_QUERY_INFORMATION | PROCESS_VM_READ
                false, process.Id);

                if (processHandle != IntPtr.Zero) {
                    StringBuilder stringBuilder = new(1024);
                    if (GetModuleFileNameEx(processHandle, IntPtr.Zero, stringBuilder, (uint)stringBuilder.Capacity) == 0) {
                        Logger.WriteLine(string.Format("Failed to get process [{0}] [{1}] full path.", process.Id, exeFile));
                    }
                    else {
                        exeFile = stringBuilder.ToString();
                    }
                    CloseHandle(processHandle);
                }
                else {
                    Logger.WriteLine(string.Format("Failed to open process [{0}] [{1}].", process.Id, exeFile));
                }

                string gameTitle = DetectionService.GetGameTitle(exeFile);

                if (!autoRecord){
                    // This is a manual record event so lets just yolo it and assume user knows best
                    RecordingService.SetCurrentSession(processId, gameTitle);
                    RecordingService.GetCurrentSession().Exe = exeFile;
                    return;
                }

                isNonGame = DetectionService.IsMatchedNonGame(exeFile);
                if (isNonGame) {
                    return;
                }

                isGame = DetectionService.IsMatchedGame(exeFile);

                if (!isGame) {
                    Logger.WriteLine(string.Format("Process [{0}] isn't in the game detection list, checking if it might be a game", Path.GetFileName(exeFile)));
                    try {
                        foreach (ProcessModule module in process.Modules) {
                            if (module == null) continue;

                            var name = module.ModuleName.ToLower();
                            modules += ", " + module.ModuleName;

                            // this could cause false positives, but it should be ok for most applications
                            if (name.StartsWith("explorerframe") || name.StartsWith("desktop-notifications") || name.StartsWith("squirrel")) { 
                                isGame = false;
                                break;
                            }

                            if (name.StartsWith("d3d") || name.StartsWith("opengl")) {
                                isGame = true;
                                Logger.WriteLine(string.Format("This process [{0}]:[{1}] : [{2}], appears to be a game.", processId, name, Path.GetFileName(exeFile)));
                            }
                            module.Dispose();
                        }
                    }
                    catch (Exception e) { // sometimes, the process locks us out from reading and throws exception (anticheat functionality?)
                        Logger.WriteLine(string.Format("Failed to view all ProcessModules for [{0}{1}] isGame: {2} isNonGame: {3}, reason: {4}", Path.GetFileName(exeFile), modules, isGame, isNonGame, e.Message));
                    }
                }
            }

            if (isGame) {
                if (!EnumerateProcessWindowHandles(processId).Any()) return;

                string gameTitle = DetectionService.GetGameTitle(exeFile);
                RecordingService.SetCurrentSession(processId, gameTitle);
                RecordingService.GetCurrentSession().Exe = exeFile;

                Logger.WriteLine(string.Format("This process [{0}] is a recordable game [{1}{2}], prepared to record", processId, Path.GetFileName(exeFile), modules));

                if (autoRecord && SettingsService.Settings.captureSettings.recordingMode == "automatic")
                    RecordingService.StartRecording();
            }
        }
    }
}

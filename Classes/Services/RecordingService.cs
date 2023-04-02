﻿using RePlays.Recorders;
using RePlays.Utils;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace RePlays.Services
{
    public static class RecordingService {
        public static BaseRecorder ActiveRecorder;

        private static Timer recordingTimer = new Timer(100);
        public static DateTime startTime;
        public static double lastVideoDuration = 0;
        private static Session currentSession = new(0, "Game Unknown");
        public static bool IsRecording { get; internal set; }
        private static bool IsPreRecording { get; set; }
        private static bool IsRestarting { get; set; }
        public static bool GameInFocus { get; set; }

        public class Session {
            public int Pid { get; internal set; }
            public string GameTitle { get; internal set; }
            public string Exe { get; internal set; }
            public Session(int _Pid, string _GameTitle, string _Exe=null) {
                Pid = _Pid;
                GameTitle = _GameTitle;
                Exe = _Exe;
            }
        }

        public static async void Start(Type type) {

            Logger.WriteLine("RecordingService starting...");
            DetectionService.Start();
            if (type == typeof(PlaysLTCRecorder))
            {
                ActiveRecorder = new PlaysLTCRecorder();
                ActiveRecorder.Start();
                return;
            }

            ActiveRecorder = new LibObsRecorder();
            Logger.WriteLine("Creating a new ActiveRecorder");
            await Task.Run(() => ActiveRecorder.Start());
            await Task.Run(() => DetectionService.CheckAlreadyRunningPrograms());
        }

        public static void SetCurrentSession(int _Pid, string _GameTitle, string exeFile) {
            currentSession = new Session(_Pid, _GameTitle, exeFile);
        }

        public static Session GetCurrentSession() {
            return currentSession;
        }

        //[STAThread]
        public static async void StartRecording() {
            Logger.WriteLine("Is PreRecording " + IsRecording.ToString());
            Logger.WriteLine("Is Recording " + IsPreRecording.ToString());

            if (IsRecording || IsPreRecording) return;

            IsPreRecording = true;
            bool result = await ActiveRecorder.StartRecording();
            Logger.WriteLine("Start Success: " + result.ToString());
            Logger.WriteLine("Still allowed to record: " + (!IsRecording && result).ToString());
            if (!IsRecording && result) {
                Logger.WriteLine("Current Session PID: " + currentSession.Pid.ToString());
                if (currentSession.Pid != 0) {
                    startTime = DateTime.Now;
                    recordingTimer.Elapsed += OnTimedEvent;
                    recordingTimer.Start();
                    
                    Logger.WriteLine($"Start Recording: {currentSession.Pid}, {currentSession.GameTitle}");
                    IsRecording = true;
                    IsPreRecording = false;
                    GameInFocus = true;

                    if (SettingsService.Settings.captureSettings.useRecordingStartSound) {
#if WINDOWS
                        System.Media.SoundPlayer startRecordingSound = new(Functions.GetResourcesFolder() + "start_recording.wav");
                        startRecordingSound.Play();
#endif
                    }
                }
            }
            if (!result) {
                // recorder failed to start properly so lets restart the currentSession Pid
                currentSession.Pid = 0;
            }
            IsPreRecording = false;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e) {
            WebMessage.DisplayToast("Recording", currentSession.GameTitle, "🔴 Recording", "none", GetTotalRecordingTimeInSeconds());
        }

        public static async void StopRecording() {
            if (!IsRecording) return;

            bool result = await ActiveRecorder.StopRecording();

            if (IsRecording && result) {
                if(currentSession.Pid != 0) {
                    recordingTimer.Elapsed -= OnTimedEvent;
                    recordingTimer.Stop();
                    Logger.WriteLine(string.Format("Stop Recording: {0}, {1}", currentSession.Pid, currentSession.GameTitle));
                    currentSession.Pid = 0;
                    WebMessage.DestroyToast("Recording");
                    IsRecording = false;
                    StorageService.ManageStorage();
                }
                //DetectionService.LoadDetections();
            }
        }
        public static async void RestartRecording() {
            if (!IsRecording || IsRestarting) return;
            IsRestarting = true;

            bool stopResult = await ActiveRecorder.StopRecording();
            bool startResult = await ActiveRecorder.StartRecording();

            if (stopResult && startResult) {
                Logger.WriteLine("Recording restart successful");
            }
            else {
                Logger.WriteLine($"Issue trying to restart recording. Could start {stopResult}, could stop {startResult}");
                IsRecording = false;
            }
            IsRestarting = false;
        }

        public static void LostFocus()
        {
            GameInFocus = false;
            ActiveRecorder.LostFocus();
        }

        public static void GainedFocus()
        {
            GameInFocus = true;
            ActiveRecorder.GainedFocus();
        }

        public static int GetTotalRecordingTimeInSeconds()
        {
            return (int)(DateTime.Now - startTime).TotalSeconds;
        }
        public static double GetTotalRecordingTimeInSecondsWithDecimals(DateTime? dateTime = null)
        {
            if(dateTime == null ) dateTime = DateTime.Now;

            return (dateTime.Value - startTime).TotalMilliseconds/1000;
        }
    }
}

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using RePlays.Utils;
using RePlays.Services;
using static RePlays.Utils.Functions;


#if !WINDOWS
using RePlays.Classes.Utils;
using RePlays.Recorders;
#else
using Squirrel;
using System.Windows.Forms;
#endif

namespace RePlays {
    static class Program {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        const int ATTACH_PARENT_PROCESS = -1;
        static readonly ManualResetEventSlim ApplicationExitEvent = new(false);

        [STAThread]
        [Obsolete]
        static void Main(string[] args) {
            Functions.SetProgramArgs(args);
            // redirect console output to parent process;
            // must be before any calls to Console.WriteLine()
            if (args.Any("--debug".Contains)) {
                Logger.IsConsole = true;
#if WINDOWS
                AttachConsole(ATTACH_PARENT_PROCESS);
#endif
            }
            else {
                Logger.Purge();
            }

            // log current culture and set culture to en-US
            Logger.WriteLine($"System Culture: {CultureInfo.CurrentCulture.Name}");
            CultureInfo.DefaultThreadCurrentCulture = new("en-US");

            // log all exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => {
                var st = new StackTrace((Exception)eventArgs.ExceptionObject, true);
                var frames = st.GetFrames();
                if (frames.Length != 0) {
                    Logger.WriteLine(
                        eventArgs.ExceptionObject.ToString(),
                        st.GetFrames().Last().GetFileName() ?? "External Library",
                        st.GetFrames().Last().GetMethod().Name,
                        st.GetFrames().Last().GetFileLineNumber()
                    );
                }
                else {
                    Logger.WriteLine(eventArgs.ExceptionObject.ToString());
                }
            };

#if WINDOWS
            // squirrel runs the exe of the version it just staged with a --squirrel-* lifecycle
            // argument and waits for it to exit. this has to happen before the single instance
            // check below: the instance being updated still owns the mutex, so the hook would
            // otherwise bail out without ever creating or removing shortcuts, and would ping the
            // old instance to the foreground for no reason.
            if (args.Any(arg => arg.StartsWith("--squirrel-", StringComparison.OrdinalIgnoreCase)) &&
                !args.Any(arg => arg.Equals("--squirrel-firstrun", StringComparison.OrdinalIgnoreCase))) {
                try {
                    SquirrelAwareApp.HandleEvents(
                        onInitialInstall: (_, tools) => tools.CreateShortcutForThisExe(),
                        onAppUpdate: (_, tools) => tools.CreateShortcutForThisExe(),
                        onAppUninstall: (_, tools) => tools.RemoveShortcutForThisExe()
                    );
                }
                catch (Exception exception) {
                    Logger.WriteLine(exception.ToString());
                }
                // HandleEvents exits the process itself, this is just a safety net so a lifecycle
                // invocation can never fall through into a second running instance
                return;
            }
#endif

            // prevent multiple instances
            var mutex = new Mutex(true, @"Global\RePlays", out var createdNew);
            if (!createdNew) {
                Logger.WriteLine("RePlays is already running! Exiting the application and bringing the other instance to foreground.");
                try {
                    using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
                        sender.Connect(new IPEndPoint(IPAddress.Loopback, 3333));
                        sender.Send(Encoding.UTF8.GetBytes("BringToForeground"));
                        Logger.WriteLine($"Sent BringToForeground to the other instance");
                    }
                }
                catch (Exception ex) {
                    Logger.WriteLine($"Socket client exception: {ex.Message}");
                }
                return;
            }

#if DEBUG && WINDOWS && !NO_SERVER
            // this will run our react app if its not already running
            var startInfo = new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = "/c npm run start",
                WorkingDirectory = Path.Join(GetSolutionPath(), @"ClientApp")
            };
            Process process = null;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost:3000/index.html");
            request.AllowAutoRedirect = false;
            request.Method = "HEAD";

            try {
                request.GetResponse();
            }
            catch (WebException) {
                process ??= Process.Start(startInfo);
            }
#endif
            SettingsService.LoadSettings();
            SettingsService.UpdateGpuManufacturer();
            SettingsService.SaveSettings();
            StorageService.ManageStorage();
            KeybindService.Start();
            PurgeTempVideos();
            Updater.CheckForUpdates();

            // Deadlock match stats can become available after the game (or RePlays)
            // closed; sweep shortly after startup and then every half hour. The first
            // sweep is delayed so the frontend is up to receive backfilled bookmarks
            // directly instead of via the backup-file path. Sweeps are near-free when
            // the pending queue is empty.
            System.Threading.Tasks.Task.Run(async () => {
                await System.Threading.Tasks.Task.Delay(30 * 1000);
                while (true) {
                    try {
                        Integrations.DeadlockIntegration.SweepPendingStats();
                    }
                    catch (Exception ex) {
                        Logger.WriteLine($"Deadlock stats sweep failed: {ex.Message}");
                    }
                    await System.Threading.Tasks.Task.Delay(30 * 60 * 1000);
                }
            });
#if WINDOWS
            ScreenSize.UpdateMaximumScreenResolution();
            // squirrel configuration; the lifecycle arguments are handled before the single
            // instance check above, this only covers a plain (or --squirrel-firstrun) launch
            try {
                SquirrelAwareApp.HandleEvents(
                    onInitialInstall: (_, tools) => tools.CreateShortcutForThisExe(),
                    onAppUpdate: (_, tools) => tools.CreateShortcutForThisExe(),
                    onAppUninstall: (_, tools) => tools.RemoveShortcutForThisExe()
                );
            }
            catch (Exception exception) {
                Logger.WriteLine(exception.ToString());
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WindowsInterface());
            (Process.GetCurrentProcess()).Kill(); // this is not a clean exit, need to look into why we can't cleanly exit
#else
            Directory.SetCurrentDirectory(AppContext.BaseDirectory); //Necessary for libobs in debug(?)
            SettingsService.LoadSettings();
            SettingsService.SaveSettings();
            // Serve video files/thumbnails to allow the frontend to use them
            WebServer.Start();
            Thread uiThread = new(LinuxInterface.Create);

            try {
                uiThread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            }
            catch (PlatformNotSupportedException ex) {
                Logger.WriteLine("PlatformNotSupportedException: " + ex.Message);
            }
            uiThread.Start();
            RecordingService.Start(typeof(LibObsRecorder));
            ApplicationExitEvent.Wait();
#endif
        }
    }
}
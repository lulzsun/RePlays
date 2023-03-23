using System;
using System.Linq;
using System.Runtime.InteropServices;
using RePlays.Utils;
using System.Threading;
using System.IO;
using System.Net;
using System.Diagnostics;
using Squirrel;
using System.Windows.Forms;
using PhotinoNET;
using RePlays.Classes.Utils;

namespace RePlays {
    static class Program {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        const int ATTACH_PARENT_PROCESS = -1;
        static readonly ManualResetEventSlim ApplicationExitEvent = new(false);
#if !WINDOWS
        public static PhotinoWindow window;
#endif

        [STAThread]
        [Obsolete]
        static void Main(string[] args) {
            // redirect console output to parent process;
            // must be before any calls to Console.WriteLine()
            string debugArg = "-debug";
            if (args.Any(debugArg.Contains)) {
                Logger.IsConsole = true;
                AttachConsole(ATTACH_PARENT_PROCESS);
            }
            else {
                Logger.Purge();
            }

            // log all exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => {
                var st = new StackTrace((Exception)eventArgs.ExceptionObject, true);
                Logger.WriteLine(eventArgs.ExceptionObject.ToString(), st.GetFrames().Last().GetFileName() ?? "External Library", st.GetFrames().Last().GetFileLineNumber());
            };

            // prevent multiple instances
            var mutex = new Mutex(true, @"Global\RePlays", out var createdNew);
            if (!createdNew) {
                Logger.WriteLine("RePlays is already running! Exiting the application.");
                return;
            }

#if DEBUG && WINDOWS   
            // this will run our react app if its not already running
            var startInfo = new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = "/c npm run start",
                WorkingDirectory = Path.Join(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, @"ClientApp")
            };
            Process process = null;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost:3000/");
            request.AllowAutoRedirect = false;
            request.Method = "HEAD";

            try {
                request.GetResponse();
            }
            catch (WebException) {
                if (process == null) process = Process.Start(startInfo);
            }
#endif

#if WINDOWS
            // squirrel configuration
            try {
                using (var manager = new UpdateManager(Environment.GetEnvironmentVariable("LocalAppData") + @"\RePlays\packages")) {
                    SquirrelAwareApp.HandleEvents(
                        onInitialInstall: v => manager.CreateShortcutForThisExe(),
                        onAppUpdate: v => manager.CreateShortcutForThisExe(),
                        onAppUninstall: v => manager.RemoveShortcutForThisExe(),
                        onFirstRun: () => Logger.WriteLine("First launch")
                    );
                }
            }
            catch (Exception exception) {
                Logger.WriteLine(exception.ToString());
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }
#else
            // this will serve video files/thumbnails to allow the react app to use them
            StaticServer.Start();
            Thread uiThread = new(OpenInterface);
            uiThread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            uiThread.Start();
            ApplicationExitEvent.Wait();
        }

        private static void OpenInterface() {
            window = new PhotinoWindow()
                .SetTitle("RePlays")
                .Center()
                .SetResizable(true)
                .RegisterWebMessageReceivedHandler(async (object sender, string message) => {
                    await WebMessage.RecieveMessage(message);
                }
            );
            window.Load($"http://localhost:3000/") // Can be used with relative path strings or "new URI()" instance to load a website.
                  .WaitForClose();
            ApplicationExitEvent.Set();
        }
#endif
    }
}
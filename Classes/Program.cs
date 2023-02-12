using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RePlays.Utils;
using System.Threading;
using Squirrel;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace RePlays {
    static class Program {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

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

#if (DEBUG) // this will run our react app if its not already running
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
                if(process == null) process = Process.Start(startInfo);
            }
#endif
            ScreenSize.UpdateMaximumScreenResolution();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }
    }
}

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using RePlays.Utils;
using System.Threading;
using Squirrel;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RePlays {
    public class Program {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        [STAThread]
        public static void Main(string[] args) {
            // redirect console output to parent process;
            // must be before any calls to Console.WriteLine()
            string debugArg = "-debug";
            if (args.Any(debugArg.Contains)) {
                Logger.IsConsole = true;
                AttachConsole(ATTACH_PARENT_PROCESS);
            }

            // log all exceptions
            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) => {
                switch (eventArgs.Exception.GetType().ToString()) {
                    case "System.Threading.Tasks.TaskCanceledException":
                    case "System.OperationCanceledException":
                        return;
                    default:
                        break;
                }
                var st = new StackTrace(eventArgs.Exception, true);
                Logger.WriteLine(eventArgs.Exception.Message.ToString(), st.GetFrame(0).GetFileName(), st.GetFrame(0).GetFileLineNumber());
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

            CreateHostBuilder(args).Build().RunAsync();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
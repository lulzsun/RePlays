using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using RePlays.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using System.Windows.Forms;

namespace RePlays {
    public class Program {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        [STAThread]
        public static void Main(string[] args) {
            if (SingleApplicationDetector.IsRunning()) {
                return;
            }

            // redirect console output to parent process;
            // must be before any calls to Console.WriteLine()
            string debugArg = "-debug";
            if (args.Any(debugArg.Contains)) {
                Logger.IsConsole = true;
                AttachConsole(ATTACH_PARENT_PROCESS);
            }

            CreateHostBuilder(args).Build().RunAsync();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());

            SingleApplicationDetector.Close();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public static class SingleApplicationDetector {
        public static bool IsRunning() {
            string guid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();
            var semaphoreName = @"Global\" + guid;
            try {
                __semaphore = Semaphore.OpenExisting(semaphoreName);
                 
                Close();
                return true;
            }
            catch (Exception ex) {
                __semaphore = new Semaphore(0, 1, semaphoreName);
                return false;
            }
        }

        public static void Close() {
            if (__semaphore != null) {
                __semaphore.Close();
                __semaphore = null;
            }
        }

        private static Semaphore __semaphore;
    }
}
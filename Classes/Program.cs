using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using RePlays.Services;
using RePlays;
using System.Threading;

namespace RePlays {
    public class Program {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        [STAThread]
        public static void Main(string[] args) {
            const string mutexName = @"Global\RePlays";

            // redirect console output to parent process;
            // must be before any calls to Console.WriteLine()
            string debugArg = "-debug";
            if (args.Any(debugArg.Contains)) {
                Logger.IsConsole = true;
                AttachConsole(ATTACH_PARENT_PROCESS);
            }

            var mutex = new Mutex(true, mutexName, out var createdNew);
            if (!createdNew) {
                Logger.WriteLine(mutexName + " is already running! Exiting the application.");
                return;
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
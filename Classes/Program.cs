using System;
using System.Linq;
using System.Runtime.InteropServices;
using RePlays.Utils;
using System.Threading;
using System.Globalization;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
#if !WINDOWS
using PhotinoNET;
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
                Logger.WriteLine(
                    eventArgs.ExceptionObject.ToString(),
                    st.GetFrames().Last().GetFileName() ?? "External Library",
                    st.GetFrames().Last().GetMethod().Name,
                    st.GetFrames().Last().GetFileLineNumber()
                );
            };

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

#if DEBUG && WINDOWS   
            // this will run our react app if its not already running
            var startInfo = new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = "/c npm run start",
                WorkingDirectory = Path.Join(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, @"ClientApp")
            };
            Process process = null;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost:3000/index.html");
            request.AllowAutoRedirect = false;
            request.Method = "HEAD";

            try {
                request.GetResponse();
            }
            catch (WebException) {
                if (process == null) process = Process.Start(startInfo);
            }
#endif
#if DEBUG || !WINDOWS
            // this will serve video files/thumbnails to allow the react app to use them
            Classes.Utils.StaticServer.Start();
#endif
#if WINDOWS
            // squirrel configuration
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
            Application.Run(new frmMain());
        }
#else
            Thread uiThread = new(OpenInterface);
            try {
                uiThread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            }
            catch (PlatformNotSupportedException ex) {
                Logger.WriteLine("PlatformNotSupportedException: " + ex.Message);
            }
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
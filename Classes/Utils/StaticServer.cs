using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using RePlays.Utils;

namespace RePlays.Classes.Utils {
    public static class StaticServer {
        static IWebHost server;
        static bool isRunning;
        public static void Start() {
            if (isRunning) {
                return;
            }
            server = WebHost.CreateDefaultBuilder(new[] { "--urls=http://localhost:3001/" })
                .Configure(config => config.UseStaticFiles(
                    new StaticFileOptions {
                        ServeUnknownFileTypes = true
                    }))
                .UseWebRoot(Functions.GetPlaysFolder()).Build();
            server.RunAsync();
            isRunning = true;
            Logger.WriteLine("Static file server started with WebRoot dir: " + Functions.GetPlaysFolder());
        }

        public static void Stop() {
            server.StopAsync();
            isRunning = false;
        }
    }
}
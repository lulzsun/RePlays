using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using RePlays.Utils;

namespace RePlays.Classes.Utils {
    public static class StaticServer {
        static IWebHost server;
        public static void Start() {
            server = WebHost.CreateDefaultBuilder(new[] { "--urls=http://localhost:3001/" })
                .Configure(config => config.UseStaticFiles())
                .UseWebRoot(Functions.GetPlaysFolder()).Build();
            server.RunAsync();
            Logger.WriteLine("Static file server started.");
        }

        public static void Stop() {
            server.StopAsync();
        }
    }
}

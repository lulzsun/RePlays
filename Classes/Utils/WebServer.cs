using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RePlays.Classes.Utils {
    public static class WebServer {
        static IWebHost server;
        static bool isRunning;
        static List<WebSocket> activeSockets = [];

        public static void Start() {
            if (isRunning) {
                return;
            }

            server = WebHost.CreateDefaultBuilder(new[] { "--urls=http://localhost:3001/" })
                .Configure(app => {
                    app.UseStaticFiles(new StaticFileOptions {
                        ServeUnknownFileTypes = true
                    });

                    // Enable WebSocket support
                    app.UseWebSockets();

                    // Map WebSocket endpoint
                    app.Use(async (context, next) => {
                        if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest) {
                            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            activeSockets.Add(webSocket);
                            await HandleWebSocket(context, webSocket);
                        }
                        else {
                            await next();
                        }
                    });
                })
                .UseWebRoot(Functions.GetPlaysFolder()).Build();
            server.RunAsync();
            isRunning = true;
            Logger.WriteLine("Static file server started with WebRoot dir: " + Functions.GetPlaysFolder());
        }

        public static void Stop() {
            server?.StopAsync();
            isRunning = false;
        }

        public static List<WebSocket> GetActiveSockets() {
            return [.. activeSockets];
        }

        private static async Task HandleWebSocket(HttpContext _, WebSocket webSocket) {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open) {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text) {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await WebMessage.RecieveMessage(receivedMessage);
                }
                else if (result.MessageType == WebSocketMessageType.Close) {
                    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    activeSockets.Remove(webSocket);
                }
            }
        }
    }
}
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using RePlays.Classes.RazorTemplates;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static RePlays.Utils.WebInterface;
using RePlays.Services;

namespace RePlays.Classes.Utils {
    public static class WebServer {
        static IWebHost server;
        static bool isRunning;
        static List<WebSocket> activeSockets = [];

        public static void Start() {
            if (isRunning) {
                return;
            }
#if RELEASE
            string webRootDir = Path.Join(Functions.GetStartupPath(), "/wwwroot/");
#else
            string webRootDir = Path.Join(Functions.GetSolutionPath(), "/wwwroot/");
#endif
            if (!Path.Exists(webRootDir)) webRootDir = Functions.GetPlaysFolder();

            server = WebHost.CreateDefaultBuilder(["--urls=http://localhost:3001/"])
                .ConfigureServices(services => {
                    services.AddRouting();
                })
                .Configure(app => {
                    // Serve videos
                    app.UseStaticFiles(new StaticFileOptions {
                        ServeUnknownFileTypes = true,
                        FileProvider = new PhysicalFileProvider(Functions.GetPlaysFolder()),
                        //RequestPath = "/videos"
                    });

                    var fsOptions = new FileServerOptions {
                        FileProvider = new PhysicalFileProvider(webRootDir),
                    };
                    fsOptions.StaticFileOptions.OnPrepareResponse = (context) => {
                        // Disable caching of all static files.
                        context.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
                        context.Context.Response.Headers.Append("Pragma", "no-cache");
                        context.Context.Response.Headers.Append("Expires", "0");
                    };

                    app.UseFileServer(fsOptions);

                    // Enable WebSocket support
                    app.UseWebSockets();

                    // Map WebSocket endpoint
                    app.Use(async (context, next) => {
                        if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest) {
                            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            activeSockets.Add(webSocket);
                            await HandleWebSocket(context, webSocket);
                        }
                        else await next();
                    });

                    app.UseRouter(routes => {
                        // Prepares index.html app
                        routes.MapGet("initialize", async context => {
                            var html = HtmlRendererFactory.RenderHtmlAsync<App>().Result;
                            await context.Response.WriteAsync(html);
                        });

                        // Retrieve videos
                        routes.MapGet("videos", async context => {
                            var userAgent = context.Request.Headers["User-Agent"].ToString();
                            var game = context.Request.Query["game"].ToString();
                            var sortBy = context.Request.Query["sortBy"].ToString();
                            var parameters = new Dictionary<string, object?> {
                                [nameof(game)] = game,
                                [nameof(sortBy)] = sortBy,
                                ["isRePlaysWebView"] = userAgent.Equals("RePlays/WebView")
                            };
                            var html = HtmlRendererFactory.RenderHtmlAsync<VideosPage>(ParameterView.FromDictionary(parameters)).Result;
                            await context.Response.WriteAsync(html);
                        });

                        // Save user settings
                        routes.MapPut("settings", async context => {
                            context.Request.EnableBuffering(); // Important for potentially reading the body multiple times

                            string requestBody = "";
                            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8)) {
                                requestBody = await reader.ReadToEndAsync();
                            }
                            Logger.WriteLine(requestBody);
                            SettingsService.SaveSetting(requestBody);

                            context.Response.StatusCode = StatusCodes.Status200OK;
                            await context.Response.WriteAsync("Ok");
                        });


                        // Map another GET route with a path parameter
                        // Note: Path parameters are usually accessed via context.GetRouteValue()
                        //routes.MapGet("greet/{name}", async context => {
                        //    var name = context.GetRouteValue("name") as string;
                        //    await context.Response.WriteAsync($"Greetings, {name} (old style)!");
                        //});

                        // Example of a POST route
                        routes.MapPost("submit", async context => {
                            context.Response.StatusCode = StatusCodes.Status200OK;
                            await context.Response.WriteAsync("Data submitted successfully (old style)!");
                        });

                        // You can also map a generic "default" route, often used for MVC controllers
                        // routes.MapRoute(
                        //     name: "default",
                        //     template: "{controller=Home}/{action=Index}/{id?}");

                        // --- CATCH-ALL ROUTE ---
                        // This route must be the LAST one defined in your UseRouter block.
                        // It uses a wildcard segment "{*path}" to match any remaining path.
                        routes.MapGet("{*path}", async context => {
                            // Important: Check if the response has already started
                            if (context.Response.HasStarted) {
                                return;
                            }

                            // Get the full path that wasn't matched by other routes
                            var unmatchedPath = context.GetRouteValue("path") as string ?? "";

                            // --- Option A: Return a 404 Not Found for API-like requests ---
                            // You might want to distinguish between a "not found API" and a "not found static file"
                            // If the original request (before API prefix middleware) started with /api/
                            // and it got here, it means no API route matched.

                            // Note: If you used the API prefix middleware, the context.Request.Path here
                            // will *not* have the /api/ prefix. You might need to check the original path
                            // or make a design decision if this catch-all is only for API 404s.

                            // For now, let's assume if it reaches here, it's a general 404
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            await context.Response.WriteAsync($"404 - Resource '{unmatchedPath}' Not Found.");


                            // --- Option B: Serve index.html for SPAs (comment out Option A if using this) ---
                            // This route will catch anything that wasn't a specific API route
                            // and wasn't served by static files (which are typically earlier in the pipeline).
                            // This is ideal for single-page applications where any unhandled route
                            // should load the main index.html file.

                            // var filePath = Path.Combine(webRootDir, "index.html");
                            // if (File.Exists(filePath))
                            // {
                            //     context.Response.ContentType = "text/html";
                            //     await context.Response.SendFileAsync(filePath);
                            // }
                            // else
                            // {
                            //     context.Response.StatusCode = StatusCodes.Status404NotFound;
                            //     await context.Response.WriteAsync($"404 - Not Found. Index file not available. Path: {unmatchedPath}");
                            // }
                        });
                    });
                })
                .Build();
            server.RunAsync();
            isRunning = true;
            Logger.WriteLine("Local web server started with WebRoot dir: " + webRootDir);
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
                    Logger.WriteLine($"Websocket message received: {receivedMessage}");
                }
                else if (result.MessageType == WebSocketMessageType.Close) {
                    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    activeSockets.Remove(webSocket);
                }
            }
        }
    }
}
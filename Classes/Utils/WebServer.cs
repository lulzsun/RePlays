using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using RePlays.Classes.RazorTemplates;
using RePlays.Classes.RazorTemplates.SettingsPages;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static RePlays.Utils.WebInterface;
using RePlays.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

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

                        async Task RenderIndexHtml(HttpContext context) {
                            using FileStream fs = new(webRootDir + "/index.html", FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
                            using StreamReader reader = new(fs, Encoding.UTF8);
                            await context.Response.WriteAsync(await reader.ReadToEndAsync());
                        }

                        routes.MapGet("/", RenderIndexHtml);
                        routes.MapGet("sessions", RenderIndexHtml);
                        routes.MapGet("clips", RenderIndexHtml);
                        routes.MapGet("player/{*path}", RenderIndexHtml);

                        // Retrieve video metadata (returns json)
                        routes.MapGet("metadata/{*path}", async context => {
                            var videoPath = "/" + context.GetRouteValue("path") ?? "";
                            var metadata = Functions.GetMetadata(Path.Join(Functions.GetPlaysFolder(), videoPath));
                            if (metadata == null) {
                                context.Response.StatusCode = StatusCodes.Status404NotFound;
                                return;
                            }
                            var json = JsonSerializer.Serialize(metadata);
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync($"{json}");
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
                            try {
                                SettingsService.SaveSetting(requestBody);
                            }
                            catch (JsonException ex) {
                                // an out of range or non numeric field should not take down the request pipeline
                                Logger.WriteLine($"Could not save setting '{requestBody}': {ex.Message}");
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                await context.Response.WriteAsync("Invalid setting");
                                return;
                            }

                            context.Response.StatusCode = StatusCodes.Status200OK;
                            await context.Response.WriteAsync("Ok");
                        });

                        // Renders the capture settings tab as an out-of-band swap. Settings that change
                        // which controls are visible (or what their options are) respond with this so the
                        // markup never drifts from the model.
                        async Task RenderCapturePage(HttpContext context) {
                            var html = HtmlRendererFactory.RenderHtmlAsync<CapturePage>().Result;
                            context.Response.ContentType = "text/html";
                            await context.Response.WriteAsync($"<div hx-swap-oob=\"innerHTML:#sett-capture-page\">{html}</div>");
                        }

                        static async Task<JsonObject> ReadJsonBody(HttpContext context) {
                            using StreamReader reader = new(context.Request.Body, Encoding.UTF8);
                            var body = await reader.ReadToEndAsync();
                            if (string.IsNullOrWhiteSpace(body)) return [];
                            return JsonNode.Parse(body)?.AsObject() ?? [];
                        }

                        routes.MapGet("settings/capture", RenderCapturePage);

                        // Video quality presets set several fields at once
                        routes.MapPut("settings/capture/preset", async context => {
                            var captureSettings = SettingsService.Settings.captureSettings;
                            var preset = (await ReadJsonBody(context))["preset"]?.ToString();
                            switch (preset) {
                                case "low":
                                    (captureSettings.resolution, captureSettings.frameRate, captureSettings.bitRate) = (480, 15, 5);
                                    break;
                                case "medium":
                                    (captureSettings.resolution, captureSettings.frameRate, captureSettings.bitRate) = (720, 30, 25);
                                    break;
                                case "high":
                                    (captureSettings.resolution, captureSettings.frameRate, captureSettings.bitRate) = (1080, 60, 35);
                                    break;
                                case "ultra":
                                    captureSettings.resolution = captureSettings.maxScreenResolution >= 1440 ? 1440 : 1080;
                                    (captureSettings.frameRate, captureSettings.bitRate) = (60, 50);
                                    break;
                                default:
                                    Logger.WriteLine($"Ignoring unknown video quality preset '{preset}'");
                                    await RenderCapturePage(context);
                                    return;
                            }
                            SettingsService.SaveSettings();
                            await RenderCapturePage(context);
                        });

                        // FileFormat is immutable, so it is picked out of the cache rather than merged field by field
                        routes.MapPut("settings/capture/fileformat", async context => {
                            var captureSettings = SettingsService.Settings.captureSettings;
                            var format = (await ReadJsonBody(context))["format"]?.ToString();
                            var fileFormat = captureSettings.fileFormatsCache.Find(f => f.format == format);
                            if (fileFormat == null) {
                                Logger.WriteLine($"Ignoring unknown file format '{format}'");
                            }
                            else {
                                captureSettings.fileFormat = fileFormat;
                                SettingsService.SaveSettings();
                            }
                            await RenderCapturePage(context);
                        });

                        // Audio devices are a list, which the settings json merge cannot patch in place,
                        // so each mutation gets its own route
                        static List<AudioDevice> GetAudioDeviceList(HttpContext context, bool cached = false) {
                            var captureSettings = SettingsService.Settings.captureSettings;
                            var isInput = context.Request.Query["isInput"].ToString() == "true";
                            if (cached) return isInput ? captureSettings.inputDevicesCache : captureSettings.outputDevicesCache;
                            return isInput ? captureSettings.inputDevices : captureSettings.outputDevices;
                        }

                        routes.MapPost("settings/capture/audiodevice", async context => {
                            var captureSettings = SettingsService.Settings.captureSettings;
                            // encoded as "input:<deviceId>" or "output:<deviceId>" by the dropdown
                            var device = (await ReadJsonBody(context))["device"]?.ToString() ?? "";
                            var isInput = device.StartsWith("input:");
                            var deviceId = device[(device.IndexOf(':') + 1)..];
                            var cache = isInput ? captureSettings.inputDevicesCache : captureSettings.outputDevicesCache;
                            var devices = isInput ? captureSettings.inputDevices : captureSettings.outputDevices;
                            var cached = cache.Find(d => d.deviceId == deviceId);

                            if (cached == null || devices.Exists(d => d.deviceId == deviceId)) {
                                Logger.WriteLine($"Ignoring add of unknown or duplicate audio device '{device}'");
                            }
                            else {
                                devices.Add(new AudioDevice(cached.deviceId, cached.deviceLabel, isInput));
                                SettingsService.SaveSettings();
                            }
                            await RenderCapturePage(context);
                        });

                        routes.MapDelete("settings/capture/audiodevice", async context => {
                            var deviceId = context.Request.Query["deviceId"].ToString();
                            if (GetAudioDeviceList(context).RemoveAll(d => d.deviceId == deviceId) > 0) {
                                SettingsService.SaveSettings();
                            }
                            await RenderCapturePage(context);
                        });

                        // Volume/denoiser do not change the shape of the page, so they respond with nothing
                        // to avoid yanking the slider out from under the user mid-drag
                        routes.MapPut("settings/capture/audiodevice", async context => {
                            var deviceId = context.Request.Query["deviceId"].ToString();
                            var device = GetAudioDeviceList(context).Find(d => d.deviceId == deviceId);
                            var body = await ReadJsonBody(context);

                            if (device == null) {
                                context.Response.StatusCode = StatusCodes.Status404NotFound;
                                await context.Response.WriteAsync("Unknown audio device");
                                return;
                            }
                            if (int.TryParse(body["volume"]?.ToString(), out var volume)) {
                                device.deviceVolume = Math.Clamp(volume, 0, 100);
                            }
                            if (bool.TryParse(body["denoiser"]?.ToString(), out var denoiser)) {
                                device.denoiser = denoiser;
                            }
                            SettingsService.SaveSettings();

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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RePlays.Classes.RazorTemplates;
using RePlays.Classes.RazorTemplates.Components;
using RePlays.Classes.Utils;
using RePlays.Services;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static RePlays.Utils.Functions;

namespace RePlays.Utils {

    public class WebInterface {

        public static Dictionary<string, string> modalList = new();
        public static Dictionary<string, string> toastList = new();
        public static VideoSortSettings videoSortSettings = new() {
            game = "All Games",
            sortBy = "Latest"
        };

        public static class HtmlRendererFactory {
            private static ServiceProvider? _serviceProvider;

            public static async Task<HtmlRenderer> CreateHtmlRendererAsync() {
                if (_serviceProvider == null) {
                    var services = new ServiceCollection();
                    services.AddLogging();
                    _serviceProvider = services.BuildServiceProvider();
                }

                var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
                return await Task.FromResult(new HtmlRenderer(_serviceProvider, loggerFactory));
            }

            public static async Task<string> RenderHtmlAsync<TComponent>() where TComponent : IComponent {
                var parameters = new Dictionary<string, object?> {
                };
                return await RenderHtmlAsync<TComponent>(ParameterView.FromDictionary(parameters));
            }

            public static async Task<string> RenderHtmlAsync<TComponent>(ParameterView parameters) where TComponent : IComponent {
                await using var htmlRenderer = await CreateHtmlRendererAsync();
                return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
                    (await htmlRenderer.RenderComponentAsync<TComponent>(parameters)).ToHtmlString()
                );
            }
        }

        public static bool SendMessage(string message, string html = "") {
            throw new Exception("Obsolete, do not use this.");

            List<WebSocket> activeSockets = WebServer.GetActiveSockets();
            foreach (var socket in activeSockets) {
                var responseMessage = Encoding.UTF8.GetBytes(message);
                Task.Run(() => {
                    socket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None);
                }).Wait();
            }
#if WINDOWS
            if (WindowsInterface.webView2 == null || WindowsInterface.webView2.IsDisposed == true || WindowsInterface.webView2.Source.AbsolutePath.Contains("preload")) return false;
            if (WindowsInterface.webView2.InvokeRequired) {
                // Call this same method but make sure it is on UI thread
                return WindowsInterface.webView2.Invoke(new Func<bool>(() => {
                    return SendMessage(message);
                }));
            }
            else {
                if (html == "") {
                    WindowsInterface.webView2.CoreWebView2.PostWebMessageAsJson(message);
                }
                else {
                    var messageData = new { message, html };
                    var jsonData = JsonSerializer.Serialize(messageData);
                    WindowsInterface.webView2.CoreWebView2.PostWebMessageAsJson(jsonData);
                }
                return true;
            }
#endif
            return true;
        }

        public static string ReceiveMessage(string message) {
            if (message == null) return null;
            Logger.WriteLine($"{message}");

            switch (message) {
                case "BrowserReady": {
                        GetAudioDevices();
                        WebServer.Start();
                        WindowsInterface.webView2.CoreWebView2.Navigate(GetRePlaysURI());
                        break;
                    }
                default:
                    break;
            }

            return message;
        }

        private static bool SendWebSocketMessage(string message) {
            List<WebSocket> activeSockets = WebServer.GetActiveSockets();
            foreach (var socket in activeSockets) {
                var responseMessage = Encoding.UTF8.GetBytes(message);
                Task.Run(() => {
                    socket.SendAsync(new ArraySegment<byte>(responseMessage), WebSocketMessageType.Text, true, CancellationToken.None);
                }).Wait();
            }
            return true;
        }

        public static void UpdateSettings() {
            var html = HtmlRendererFactory.RenderHtmlAsync<SettingsPage>().Result;
            SendWebSocketMessage(html);
        }

        public static void UpdateVideos() {
            var html = """
                <div hx-swap-oob="outerHTML:#update-videos" id="update-videos" hx-get="videos" hx-vals='{"game": "All Games", "sortBy": "Latest"}' hx-trigger="load"/>
                """;
            SendWebSocketMessage(html);
        }

        public static async void DisplayModal(string context, string title = "Title", string icon = "none") {
            var parameters = new Dictionary<string, object?> {
                [nameof(context)] = context,
                [nameof(title)] = title,
                [nameof(icon)] = icon,
            };
            var html = HtmlRendererFactory.RenderHtmlAsync<Modal>(ParameterView.FromDictionary(parameters)).Result;

            bool success = SendWebSocketMessage(html);
            if (!success) {
                // if message was not successful (interface was probably minimized), save to list to show later
                modalList.Add(context, html);
            }
        }

        public static void DisplayToast(string id, string context, string title = "Title", string icon = "none", long progress = 0, long progressMax = 0) {
            var parameters = new Dictionary<string, object?> {
                [nameof(id)] = id,
                [nameof(context)] = context,
                [nameof(title)] = title,
                [nameof(icon)] = icon,
                [nameof(progress)] = progress,
                [nameof(progressMax)] = progressMax
            };
            var html = HtmlRendererFactory.RenderHtmlAsync<Toast>(ParameterView.FromDictionary(parameters)).Result;

            if (toastList.ContainsKey(id)) {
                if (toastList[id] == html) return; // prevents message flooding if toast is identical
                toastList[id] = html;
            }
            else toastList.Add(id, html);

            SendWebSocketMessage(html);
        }

        public static void DestroyToast(string id) {
            if (toastList.ContainsKey(id))
                toastList.Remove(id);

            var parameters = new Dictionary<string, object?> {
                [nameof(id)] = id,
                ["context"] = "",
            };
            var html = HtmlRendererFactory.RenderHtmlAsync<Toast>(ParameterView.FromDictionary(parameters)).Result;
            SendWebSocketMessage(html);
        }

        public static void SetBookmarks(string videoName, List<Bookmark> bookmarks, double elapsed) {
            string json = "{" +
                    "\"videoname\": \"" + videoName + "\", " +
                    "\"elapsed\": " + elapsed.ToString().Replace(",", ".") + ", " +
                    "\"bookmarks\": " + JsonSerializer.Serialize(bookmarks) + "}";

            //if (WindowsInterface.webView2 != null) {
            //    WebMessage webMessage = new();
            //    webMessage.message = "SetBookmarks";
            //    webMessage.data = json;
            //    SendMessage(JsonSerializer.Serialize(webMessage));
            //    Logger.WriteLine("Successfully sent bookmarks to frontend");
            //}
            //else {
            //    BackupBookmarks(videoName, json);
            //}
        }
    }
}
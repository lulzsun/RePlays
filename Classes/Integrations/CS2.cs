using Newtonsoft.Json;
using RePlays.Services;
using RePlays.Utils;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RePlays.Integrations {
    internal class CS2 : Integration {

        private State oldState;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private HttpListener listener = new HttpListener();

        public override async Task Start() {
            InitializeListener();

            while (listener.IsListening) {
                try {
                    HttpListenerContext context = await listener.GetContextAsync();

                    await semaphore.WaitAsync();

                    try {
                        await HandleRequest(context);
                    }
                    finally {
                        semaphore.Release();
                    }
                }
                catch (Exception ex) {
                    Logger.WriteLine(RecordingService.IsRecording ? $"The listener has stopped unexpectedly. Error {ex.Message}" : "The listener has stopped successfully");
                    break;
                }
            }
        }

        private async Task HandleRequest(HttpListenerContext context) {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.HttpMethod == "POST") {
                var body = await ReadRequestBodyAsync(request);
                State newState = DeserializeState(body);

                // If there is a new kill and the match is live
                if (IsValidState(newState, oldState)) {
                    BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Kill });
                    oldState = newState;
                }

                // Reset stats
                if (HasPhaseChanged(newState, oldState)) {
                    oldState = newState;
                }

                await WriteResponseAsync(response);
            }

            response.Close();
        }
        private void InitializeListener() {
            Logger.WriteLine("Starting CS2 integration");
            oldState = new State();
            listener.Prefixes.Add("http://127.0.0.1:1337/");
            listener.Start();
            Logger.WriteLine("Listening at http://127.0.0.1:1337/");
        }

        private static async Task<string> ReadRequestBodyAsync(HttpListenerRequest request) {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        private static State DeserializeState(string body) {
            return JsonConvert.DeserializeObject<State>(body);
        }

        static async Task WriteResponseAsync(HttpListenerResponse response) {
            byte[] buffer = Encoding.UTF8.GetBytes("");
            response.ContentLength64 = buffer.Length;
            using (Stream output = response.OutputStream) {
                await output.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        private static bool IsValidState(State newState, State oldState) {
            return newState?.Player?.MatchStats != null &&
                   newState.Player.SteamId == newState.Provider.SteamId &&
                   newState.Player.MatchStats.Kills > oldState?.Player?.MatchStats?.Kills &&
                   newState.Map.Phase == "live";
        }

        private static bool HasPhaseChanged(State newState, State oldState) {
            return newState?.Map?.Phase != oldState?.Map?.Phase;
        }

        public async override Task Shutdown() {
            Logger.WriteLine("Shutting down CS2 integration");
            listener.Close();
        }

        public class MatchStats {
            [JsonProperty("kills")]
            public int Kills { get; set; }
        }

        public class Player {
            [JsonProperty("steamid")]
            public string SteamId { get; set; }

            [JsonProperty("match_stats")]
            public MatchStats MatchStats { get; set; }
        }

        public class Provider {
            [JsonProperty("steamid")]
            public string SteamId { get; set; }

        }

        public class Map {
            [JsonProperty("phase")]
            public string Phase { get; set; }
        }

        public class State {
            [JsonProperty("player")]
            public Player Player { get; set; }
            [JsonProperty("provider")]
            public Provider Provider { get; set; }
            [JsonProperty("map")]
            public Map Map { get; set; }
        }
    }
}


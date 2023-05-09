using RePlays.Services;
using RePlays.Utils;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace RePlays.Integrations {
    internal class LeagueOfLegendsIntegration : Integration {
        Timer timer = new Timer() {
            Interval = 250,
        };

        public override async Task Start() {
            Logger.WriteLine("Starting League Of Legends integration");
            int lastKills = 0;
            string username = await GetCurrentPlayerName();

            if (username == null) {
                Logger.WriteLine("Could not get the current player name");
                return;
            }

            timer.Elapsed += async (sender, e) => {
                using (var handler = new HttpClientHandler {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                })
                using (HttpClient client = new HttpClient(handler)) {
                    try {
                        string result = await client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/playerscores?summonerName=" + username);
                        JsonDocument doc = JsonDocument.Parse(result);
                        JsonElement root = doc.RootElement;

                        int currentKills = root.GetProperty("kills").GetInt32();
                        if (currentKills != lastKills) {
                            BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Kill });
                            Console.WriteLine("Kills changed to: " + currentKills);
                            lastKills = currentKills;
                        }
                    }
                    catch {
                        if (!RecordingService.IsRecording || RecordingService.GetTotalRecordingTimeInSeconds() > 180) {
                            timer.Stop();
                            await Shutdown();
                        }
                    }
                }
            };
            timer.Start();
            Logger.WriteLine("Successfully started League Of Legends integration");
        }

        public async override Task Shutdown() {
            if (timer.Enabled) {
                Logger.WriteLine("Shutting down League Of Legends integration");
                timer.Stop();
            }
        }

        private async Task<string> GetCurrentPlayerName() {
            using (var handler = new HttpClientHandler {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            })
            using (HttpClient client = new HttpClient(handler)) {
                while (true) {
                    try {
                        return (await client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/activeplayername")).Trim('\"');
                    }
                    catch (Exception ex) {
                        if (RecordingService.IsRecording && RecordingService.GetTotalRecordingTimeInSeconds() > 180) {
                            Logger.WriteLine("Could not get player name, error: " + ex.Message);
                            return null;
                        }
                        await Task.Delay(5000);
                    }
                }
            }
        }

    }
}

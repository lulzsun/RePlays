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

        public static PlayerStats stats;

        public override async Task Start() {
            Logger.WriteLine("Starting League Of Legends integration");
            stats = new PlayerStats();

            timer.Elapsed += async (sender, e) => {
                using (var handler = new HttpClientHandler {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                })
                using (HttpClient client = new HttpClient(handler)) {
                    try {
                        string result = await client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/playerlist");
                        JsonDocument doc = JsonDocument.Parse(result);
                        JsonElement root = doc.RootElement[0];
                        int currentKills = root.GetProperty("scores").GetProperty("kills").GetInt32();
                        if (currentKills != stats.Kills) {
                            BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Kill });
                            Console.WriteLine("Kills changed to: " + currentKills);
                        }

                        stats.Deaths = root.GetProperty("scores").GetProperty("deaths").GetInt32();
                        stats.Assists = root.GetProperty("scores").GetProperty("assists").GetInt32();
                        stats.Kills = currentKills;
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
    }
}

public class PlayerStats {
    public int Kills { get; set; }
    public int Assists { get; set; }
    public int Deaths { get; set; }
}

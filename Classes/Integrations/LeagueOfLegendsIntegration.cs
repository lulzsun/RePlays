using RePlays.Services;
using RePlays.Utils;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;


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
                        string result = await client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/allgamedata");
                        JsonDocument doc = JsonDocument.Parse(result);
                        JsonElement root = doc.RootElement;

                        string username = root.GetProperty("activePlayer").GetProperty("summonerName").GetString();

                        // Parsing all players
                        JsonElement allPlayers = root.GetProperty("allPlayers");
                        JsonElement currentPlayer = allPlayers
                            .EnumerateArray()
                            .FirstOrDefault(playerElement => playerElement.GetProperty("summonerName").GetString() == username);

                        int currentKills = currentPlayer.GetProperty("scores").GetProperty("kills").GetInt32();
                        if (currentKills != stats.Kills) {
                            BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Kill });
                            Console.WriteLine("Kills changed to: " + currentKills);
                        }

                        int currentDeaths = currentPlayer.GetProperty("scores").GetProperty("deaths").GetInt32();
                        if (currentDeaths != stats.Deaths) {
                            BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Death });
                            Console.WriteLine("Deaths changed to: " + currentDeaths);
                        }

                        int currentAssists = currentPlayer.GetProperty("scores").GetProperty("assists").GetInt32();
                        if (currentAssists != stats.Assists) {
                            BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Assist });
                            Console.WriteLine("Assists changed to: " + currentAssists);
                        }

                        stats.Kills = currentKills;
                        stats.Deaths = currentDeaths;
                        stats.Assists = currentAssists;
                        stats.Champion = currentPlayer.GetProperty("rawChampionName").GetString().Replace("game_character_displayname_", "");
                        stats.Win = root.GetProperty("events").GetProperty("Events")
                            .EnumerateArray()
                            .Where(eventElement => eventElement.GetProperty("EventName").GetString() == "GameEnd")
                            .Any(eventElement => eventElement.GetProperty("Result").GetString() == "Win");

                    }
                    catch (Exception ex) {
                        Logger.WriteLine(ex.Message);
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
    public string Champion { get; set; }
    public bool Win { get; set; }
}

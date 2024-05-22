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
        Timer timer = new() {
            Interval = 250,
        };

        public static PlayerStats stats;

        public override Task Start() {
            Logger.WriteLine("Starting League Of Legends integration");
            stats = new PlayerStats();

            timer.Elapsed += async (sender, e) => {
                using var handler = new HttpClientHandler {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                using HttpClient client = new(handler);
                try {
                    string result = await client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/allgamedata");
                    JsonDocument doc = JsonDocument.Parse(result);
                    JsonElement root = doc.RootElement;

                    if (!root.TryGetProperty("events", out JsonElement eventList)) {
                        return;
                    }
                    else {
                        if (!eventList.TryGetProperty("Events", out JsonElement events)) {
                            return;
                        }
                        else {
                            if (!events.EnumerateArray().Any(
                                element => element.TryGetProperty("EventName", out JsonElement propertyValue) &&
                                propertyValue.GetString() == "GameStart")) {
                                return;
                            }
                        }
                    }

                    string username = root.GetProperty("activePlayer").GetProperty("riotId").GetString();

                    // Parsing all players
                    JsonElement allPlayers = root.GetProperty("allPlayers");
                    JsonElement currentPlayer = allPlayers
                        .EnumerateArray()
                        .FirstOrDefault(playerElement => playerElement.GetProperty("riotId").GetString() == username);

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
                    stats.Champion = currentPlayer.GetProperty("rawChampionName").GetString().Replace("game_character_displayname_", "").Replace("FiddleSticks", "Fiddlesticks");
                    stats.Win = root.GetProperty("events").GetProperty("Events")
                        .EnumerateArray()
                        .Where(eventElement => eventElement.GetProperty("EventName").GetString() == "GameEnd")
                        .Any(eventElement => eventElement.GetProperty("Result").GetString() == "Win");

                }
                catch (Exception ex) {
                    if (ex.GetType() != typeof(HttpRequestException)) {
                        Logger.WriteLine(ex.ToString());
                    }
                    if (!RecordingService.IsRecording || RecordingService.GetTotalRecordingTimeInSeconds() > 180) {
                        timer.Stop();
                        await Shutdown();
                    }

                }
            };
            timer.Start();
            Logger.WriteLine("Successfully started League Of Legends integration");
            return Task.CompletedTask;
        }

        public override Task Shutdown() {
            if (timer.Enabled) {
                Logger.WriteLine("Shutting down League Of Legends integration");
                timer.Stop();
            }

            return Task.CompletedTask;
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
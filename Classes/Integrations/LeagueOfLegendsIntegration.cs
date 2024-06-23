using RePlays.Services;
using RePlays.Utils;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;


namespace RePlays.Integrations {
    internal class LeagueOfLegendsIntegration : Integration {
        static Timer timer;

        public PlayerStats stats;

        public override Task Start() {
            Logger.WriteLine("Starting League Of Legends integration");
            stats = new PlayerStats();
            timer = new() {
                Interval = 250,
            };

            timer.Elapsed += async (sender, e) => {
                using var handler = new HttpClientHandler {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                string result = "";
                using HttpClient client = new(handler);
                try {
                    result = await client.GetStringAsync("https://127.0.0.1:2999/liveclientdata/allgamedata");
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

                    string username = "";

                    // lulzsun: now, i no longer play LOL so i am not exactly sure of this,
                    // but how come api documentation sample does not use 'riotId'? is it wrong or 
                    // is it not updated?
                    // https://static.developer.riotgames.com/docs/lol/liveclientdata_sample.json
                    // just incase, we will fallback to using the 'summonerName' property if it fails
                    if (root.TryGetProperty("activePlayer", out JsonElement activePlayer) &&
                        activePlayer.TryGetProperty("riotId", out JsonElement id)) {
                        username = id.GetString();
                    }
                    else if (root.TryGetProperty("activePlayer", out activePlayer) &&
                        activePlayer.TryGetProperty("summonerName", out id)) {
                        username = id.GetString();
                    }

                    // Parsing all players
                    JsonElement allPlayers = root.GetProperty("allPlayers");
                    JsonElement currentPlayer = allPlayers
                        .EnumerateArray()
                        .FirstOrDefault(playerElement => {
                            // lulzsun: same issue from above applies here...
                            if (playerElement.TryGetProperty("riotId", out JsonElement id)) {
                                return id.GetString() == username;
                            }
                            return playerElement.GetProperty("summonerName").GetString() == username;
                        });

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
                        Logger.WriteLine("Provided json: " + Regex.Replace(result, @"\n|\r\n", ""));
                        // just shutdown at this point, its probably broken
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
                timer.Stop();
                timer.Dispose();
                Logger.WriteLine("Shutting down League Of Legends integration");
            }
            else {
                Logger.WriteLine("Already shutdown League Of Legends integration!");
            }
            return Task.CompletedTask;
        }

        public void UpdateMetadataWithStats(string videoPath) {
            string thumbsDir = Path.Combine(Path.GetDirectoryName(videoPath), ".thumbs/");
            string metadataPath = Path.Combine(thumbsDir, Path.GetFileNameWithoutExtension(videoPath) + ".metadata");
            if (File.Exists(metadataPath)) {
                VideoMetadata metadata = JsonSerializer.Deserialize<VideoMetadata>(File.ReadAllText(metadataPath));
                metadata.kills = stats.Kills;
                metadata.assists = stats.Assists;
                metadata.deaths = stats.Deaths;
                metadata.champion = stats.Champion;
                metadata.win = stats.Win;
                File.WriteAllText(metadataPath, JsonSerializer.Serialize<VideoMetadata>(metadata));
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
}
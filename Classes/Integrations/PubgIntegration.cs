using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RePlays.Integrations {
    internal class PubgIntegration : Integration {
        public class MatchData {
            public int LengthInMS { get; set; }
            public int NetworkVersion { get; set; }
            public int Changelist { get; set; }
            public string FriendlyName { get; set; }
            public int DemoFileLastOffset { get; set; }
            public int SizeInBytes { get; set; }
            public long Timestamp { get; set; } //Start of the game
            public bool bIsLive { get; set; }
            public bool bIncomplete { get; set; }
            public bool bIsServerRecording { get; set; }
            public bool bShouldKeep { get; set; }
            public string GameVersion { get; set; }
            public string Mode { get; set; }
            public string RecordUserId { get; set; }
            public string RecordUserNickName { get; set; } //Current players username
            public string MapName { get; set; }
            public bool bAllDeadOrWin { get; set; }
            public List<object> Reports { get; set; }
            public bool bIsClip { get; set; }
            public int ClipTime { get; set; }
            public int ClipStartTime { get; set; }
            public int ClipEndTime { get; set; }
            public string ClipTargetUserId { get; set; }
            public string ClipTargetUserNickName { get; set; }
            public string ReportToken { get; set; }
            public int MK3DReplayVerstion { get; set; }
        }


        public class DataOverview {
            public string id { get; set; }
            public string group { get; set; }
            public string meta { get; set; }
            public int time1 { get; set; } //milliseconds since MatchData.Timestamp
            public int time2 { get; set; }
            public string data { get; set; } //ASCII Shift Cipher encrypted game data
        }

        public class KillPlayerData {
            [JsonPropertyName("*e8199c40d3")]
            public string killerNetId { get; set; }
            [JsonPropertyName("*3a79bf88c2")]
            public string killerName { get; set; }
            public string victimNetId { get; set; }
            [JsonPropertyName("*c7a98fdd12")]
            public string victimName { get; set; }
            [JsonPropertyName("*83180ea500")]
            public string damageCauseClassName { get; set; }
            [JsonPropertyName("*834975f626")]
            public string damageTypeCategory { get; set; }
            [JsonPropertyName("*532cd7df97")]
            public string damageReason { get; set; }
            [JsonPropertyName("*967d42d4a0")]
            public bool bGroggy { get; set; }
            [JsonPropertyName("*af4e5308f0")]
            public string killerPlayerId { get; set; }
            [JsonPropertyName("*7441984e8b")]
            public string victimPlayerId { get; set; }
        }

        public class DownedData {
            [JsonPropertyName("*e8199c40d3")]
            public string instigatorNetId { get; set; }
            [JsonPropertyName("*3a79bf88c2")]
            public string instigatorName { get; set; }
            public string victimNetId { get; set; }
            [JsonPropertyName("*c7a98fdd12")]
            public string victimName { get; set; }
            [JsonPropertyName("*83180ea500")]
            public string damageCauseClassName { get; set; }
            [JsonPropertyName("*834975f626")]
            public string damageTypeCategory { get; set; }
            [JsonPropertyName("*532cd7df97")]
            public string damageReason { get; set; }
            [JsonPropertyName("*967d42d4a0")]
            public bool bGroggy { get; set; }
            [JsonPropertyName("*af4e5308f0")]
            public string instigatorPlayerId { get; set; }
            [JsonPropertyName("*7441984e8b")]
            public string victimPlayerId { get; set; }
        }

        System.Timers.Timer timer = new System.Timers.Timer() {
            Interval = 2500,
        };
        private readonly string demoDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"TslGame\Saved\Demos");
        private HashSet<string> oldDemos;
        public override Task Start() {
            oldDemos = Directory.GetDirectories(demoDirectory).ToHashSet();
            Logger.WriteLine("Starting PUBG integration");
            timer.Elapsed += (sender, e) => {
                try {
                    HashSet<string> demos = Directory.GetDirectories(demoDirectory).ToHashSet();
                    if (demos.SetEquals(oldDemos))
                        return;
                    HashSet<string> newDemos = demos.Where(x => !oldDemos.Any(y => y == x)).ToHashSet();
                    oldDemos = demos;

                    foreach (var demoPath in newDemos) {
                        Logger.WriteLine("Found new PUBG match data: " + demoPath);
                        HashSet<string> appliedBookmarks = new HashSet<string>();

                        //Get match data
                        string json = GetJsonFromFile(Path.Combine(demoPath, @"PUBG.replayinfo"));
                        MatchData matchData = JsonSerializer.Deserialize<MatchData>(json);

                        AddDownedBookmarks(demoPath, matchData, appliedBookmarks);
                        AddKillsBookmarks(demoPath, matchData, appliedBookmarks);
                    }
                }
                catch (Exception ex) {
                    Logger.WriteLine("PUBG integration error: " + ex.Message);
                }
            };
            timer.Start();
            Logger.WriteLine("Successfully started PUBG integration");
            return Task.CompletedTask;
        }

        public override Task Shutdown() {
            //Wait two second to make sure the bookmark file has been added 
            Logger.WriteLine("Shutting down PUBG integration");
            timer.Stop();
            return Task.CompletedTask;
        }

        private void AddDownedBookmarks(string demoPath, MatchData matchData, HashSet<string> appliedBookmarks) {
            //All downing are saved as individual files (ex. groggy0, groggy1, groggy3)
            string[] downedMetaFiles = Directory.GetFiles(demoPath + @"\events", "groggy*");
            Logger.WriteLine("Found " + downedMetaFiles.Length + " downed players");
            foreach (string downedPlayerFilePath in downedMetaFiles) {
                //Get (encrypted) event data
                string json = GetJsonFromFile(downedPlayerFilePath);
                DataOverview dataOverview = JsonSerializer.Deserialize<DataOverview>(json);

                //Decrypt data
                string cipherEncodedData = Encoding.UTF8.GetString(Convert.FromBase64String(dataOverview.data));
                DownedData downedData = JsonSerializer.Deserialize<DownedData>(DecryptAsciiShiftCipher(cipherEncodedData));

                //If current user downed someone (not themselves)
                if (downedData.instigatorName == matchData.RecordUserNickName && downedData.victimName != matchData.RecordUserNickName) {
                    Logger.WriteLine(downedData.instigatorName + " downed " + downedData.victimName);

                    //DateTime at the event (downing)
                    DateTime bookmarkDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeMilliseconds(matchData.Timestamp + dataOverview.time1).DateTime, TimeZoneInfo.Local);

                    //Add to a HashSet to make sure that RePlays bookmarks the actual downing (not the kill)
                    appliedBookmarks.Add(downedData.victimName);

                    Bookmark bookmark = new() {
                        type = Bookmark.BookmarkType.Kill
                    };
                    BookmarkService.AddBookmark(bookmark, bookmarkDateTime);
                }
            }
        }

        private void AddKillsBookmarks(string demoPath, MatchData matchData, HashSet<string> appliedBookmarks) {
            //All kills are saved as individual files (ex. kill0, kill1, kill3)
            string[] killsMetaFiles = Directory.GetFiles(demoPath + @"\events", "kill*");
            Logger.WriteLine("Found " + killsMetaFiles.Length + " kills");
            foreach (string killFilePath in killsMetaFiles) {
                //Get (encrypted) event data
                string json = GetJsonFromFile(killFilePath);
                DataOverview killDataOverview = JsonSerializer.Deserialize<DataOverview>(json);

                //Decrypt data
                string cipherEncodedData = Encoding.UTF8.GetString(Convert.FromBase64String(killDataOverview.data));
                KillPlayerData killData = JsonSerializer.Deserialize<KillPlayerData>(DecryptAsciiShiftCipher(cipherEncodedData));

                //If current user kills someone (not themselves)
                if (killData.killerName == matchData.RecordUserNickName && killData.victimName != matchData.RecordUserNickName) {
                    Logger.WriteLine(killData.killerName + " killed " + killData.victimName);

                    //DateTime at the event (kill)
                    DateTime bookmarkDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeMilliseconds(matchData.Timestamp + killDataOverview.time1).DateTime, TimeZoneInfo.Local);
                    bool killedDirectlyWithoutDowning = appliedBookmarks.Add(killData.victimName);

                    //Only add the kill if I haven't downed the person before (this is known as an instant kill where the victim is alone left in their team)
                    //This is to prevent bookmarks at both downing and killing.
                    if (killedDirectlyWithoutDowning) {
                        Bookmark bookmark = new() {
                            type = Bookmark.BookmarkType.Kill
                        };
                        BookmarkService.AddBookmark(bookmark, bookmarkDateTime);
                    }
                }
            }
        }


        private string GetJsonFromFile(string fileLocation) {
            //The file includes random characters at the start and end
            string json = File.ReadAllText(fileLocation);
            int jsonStartIndex = json.IndexOf("{");
            int jsonEndIndex = json.LastIndexOf("}") + 1;
            return json.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex);
        }

        //PUBG uses ASCII Shift Cipher to encode data 
        private string DecryptAsciiShiftCipher(string encryptedText) {
            int shift = 93;
            string decryptedText = "";
            foreach (char c in encryptedText) {
                if (c >= '!' && c <= '|') {
                    int code = (int)c - shift;
                    if (code < (int)'!') {
                        code += 94;
                    }
                    decryptedText += (char)code;
                }
                else {
                    decryptedText += c;
                }
            }

            return decryptedText;
        }

    }
}
using Microsoft.Win32;
using Newtonsoft.Json;
using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RePlays.Integrations {
    internal class RainbowSixIntegration : Integration {
        private static readonly string TempFolder = Functions.GetTempFolder();
        private static readonly string SteamGameId = "359550";
        private static readonly string UbisoftGameId = "635";
        private static string R6DissectVersion;
        private static string R6DissectFileName;
        private static string R6DissectZipUrl;
        private static string R6DissectMd5Url;
        private static readonly string R6DissectZipPath = Path.Combine(Functions.GetTempFolder(), "r6-dissect.zip");
        private static readonly string R6DissectExtractionPath = Path.Combine(Functions.GetTempFolder(), "r6-dissect");
        private static readonly string R6DissectExecutable = Path.Combine(R6DissectExtractionPath, "r6-dissect.exe");
        private static readonly string MD5FilePath = Path.Combine(Functions.GetTempFolder(), "r6-dissect", "md5.txt");

        internal class Player {
            [JsonProperty("profileID")]
            internal string ProfileID { get; set; }

            [JsonProperty("username")]
            internal string Username { get; set; }
        }

        internal class EventType {
            [JsonProperty("name")]
            internal string Name { get; set; }
        }

        internal class MatchFeedback {
            [JsonProperty("username")]
            internal string Username { get; set; }

            [JsonProperty("target")]
            internal string Target { get; set; }

            [JsonProperty("timeInSeconds")]
            internal int TimeInSeconds { get; set; }

            [JsonProperty("type")]
            internal EventType Type { get; set; }
        }

        internal class Round {
            [JsonProperty("timestamp")]
            internal DateTime Timestamp { get; set; }

            [JsonProperty("recordingProfileID")]
            internal string RecordingProfileID { get; set; }

            [JsonProperty("players")]
            internal List<Player> Players { get; set; }

            [JsonProperty("matchFeedback")]
            internal List<MatchFeedback> MatchFeedback { get; set; }

            [JsonProperty("matchType")]
            internal MatchType MatchType { get; set; }
        }

        internal class MatchType {
            [JsonProperty("name")]
            internal string Name { get; set; }

            [JsonProperty("id")]
            internal int Id { get; set; }
            internal MatchTypeEnum MatchTypeEnum {
                get {
                    return (MatchTypeEnum)Id;
                }
            }
        }
        internal enum MatchTypeEnum {
            QuickMatch = 1,
            Ranked = 2,
            CustomGameLocal = 3,
            CustomGameOnline = 4,
            Standard = 8
        }

        internal class ReleaseInfo {
            [JsonProperty("tag_name")]
            internal string TagName { get; set; }
        }


        public class MatchData {
            public List<Round> Rounds { get; set; }

            internal (List<DateTime> killTimestamps, List<DateTime> deathTimestamps) GetKillAndDeathTimestamps(string recordingProfileID) {
                var killTimestamps = new List<DateTime>();
                var deathTimestamps = new List<DateTime>();

                // segergren: The round timer resets to 45 seconds when the defuser is planted.
                // There is an issue with r6-dissect that prevents us from accurately calculating the kill time after the defuser is planted.
                // This is a known limitation and acceptable (for now!) since the defuser often isn't planted.
                // I'll address it once https://github.com/redraskal/r6-dissect/issues/102 is resolved.
                foreach (var round in Rounds) {
                    int matchLength = round.MatchType.MatchTypeEnum == MatchTypeEnum.QuickMatch ? 195 : 225;
                    var playerKills = round.MatchFeedback
                        .Where(mf => mf.Type.Name == "Kill" && round.Players.Any(p => p.ProfileID == recordingProfileID && p.Username == mf.Username))
                        .Select(mf => round.Timestamp.AddSeconds(matchLength - mf.TimeInSeconds))
                        .ToList();

                    var playerDeaths = round.MatchFeedback
                        .Where(mf => mf.Type.Name == "Kill" && round.Players.Any(p => p.ProfileID == recordingProfileID && p.Username == mf.Target))
                        .Select(mf => round.Timestamp.AddSeconds(matchLength - mf.TimeInSeconds))
                        .ToList();

                    killTimestamps.AddRange(playerKills);
                    deathTimestamps.AddRange(playerDeaths);
                }

                return (killTimestamps, deathTimestamps);
            }
        }

        public override async Task Start() {
            try {
                bool isDownloadNeeded = await CheckIfDownloadNeeded();
                if (isDownloadNeeded) {
                    await DownloadAndExtractR6Dissect();
                }
            }
            catch (Exception ex) {
                Logger.WriteLine($"Error during Rainbow Six Siege integration: {ex.Message}");
                IntegrationService.Shutdown();
            }

            try {
                DeleteOldMatchFiles();
            }
            catch (Exception ex) {
                Logger.WriteLine($"Error deleting old match files: {ex.Message}");
            }
        }

        public override Task Shutdown() {
            try {
                if (!File.Exists(R6DissectExecutable)) {
                    Logger.WriteLine("r6-dissect.exe not found.");
                    return Task.CompletedTask;
                }

                string matchReplayDirectory = GetMatchReplayDirectory();
                int matchIndex = 0;
                var directories = Directory.GetDirectories(matchReplayDirectory);
                var matchesInCurrentRecordingSession = directories
                    .Where(dir => Directory.GetLastWriteTime(dir) >= RecordingService.startTime)
                    .ToList();
                Logger.WriteLine($"Found {directories.Length} matches in total. {matchesInCurrentRecordingSession.Count} matches are from the current recording session.");

                foreach (var directory in matchesInCurrentRecordingSession) {
                    string outputFilePath = Path.Combine(TempFolder, "match_" + matchIndex + ".json");
                    string convertRecToJsonCommand = $"{R6DissectExecutable.Replace('\\', '/')} \"{directory.Replace('\\', '/')}\" -o \"{outputFilePath.Replace('\\', '/')}\"";
                    string directoryName = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                    Logger.WriteLine($"Processing {directoryName}.");
                    RunCommand(convertRecToJsonCommand);
                    Logger.WriteLine($"Done converting {directoryName} to JSON-file.");
                    matchIndex++;
                }

                ProcessMatchData();
                return Task.CompletedTask;
            }
            catch (Exception ex) {
                Logger.WriteLine($"Rainbow Six integration shutdown error. Exception: {ex.ToString()}");
                return Task.CompletedTask;
            }
        }

        private string GetMatchReplayDirectory() {
            // Check for the Ubisoft version
            string ubisoftRegistryKeyPath = $@"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\{UbisoftGameId}";
            string installDirValueName = "InstallDir";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(ubisoftRegistryKeyPath)) {
                if (key != null) {
                    object installDir = key.GetValue(installDirValueName);
                    if (installDir != null) {
                        Logger.WriteLine("Found Ubisoft MatchReplay directory");
                        return Path.Combine(installDir.ToString(), "MatchReplay");
                    }
                }
            }

            // Check for the Steam version
            string steamRegistryKeyPath = @"SOFTWARE\WOW6432Node\Valve\Steam";
            string steamInstallDirValueName = "InstallPath";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(steamRegistryKeyPath)) {
                if (key != null) {
                    object steamInstallDir = key.GetValue(steamInstallDirValueName);
                    if (steamInstallDir != null) {
                        string steamPath = steamInstallDir.ToString();
                        string libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

                        if (File.Exists(libraryFoldersPath)) {
                            string libraryFoldersContent = File.ReadAllText(libraryFoldersPath);

                            var libraryRegex = new Regex(@"""path""\s+""(.+?)"".+?""apps""\s+\{([^}]*)\}", RegexOptions.Singleline);
                            var appRegex = new Regex(@"""(\d+)""", RegexOptions.Singleline);

                            var matches = libraryRegex.Matches(libraryFoldersContent);
                            foreach (Match match in matches) {
                                string libraryPath = match.Groups[1].Value.Replace("\\\\", "\\");
                                string appsContent = match.Groups[2].Value;

                                // Check if 359550 is installed in this library
                                if (appRegex.IsMatch(appsContent) && appRegex.Match(appsContent).Groups[1].Value == SteamGameId) {
                                    Logger.WriteLine("Found Steam MatchReplay directory");
                                    return Path.Combine(libraryPath, "steamapps", "common", "Tom Clancy's Rainbow Six Siege", "MatchReplay");
                                }
                            }
                        }
                    }
                }
            }

            throw new DirectoryNotFoundException("Could not find the game install directory for either Ubisoft or Steam version in the registry.");
        }

        private static async Task<bool> CheckIfDownloadNeeded() {
            using (HttpClient client = new HttpClient()) {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("RePlays");
                Logger.WriteLine("Getting r6-dissect latest release");
                var releaseResponse = await client.GetStringAsync("https://api.github.com/repos/redraskal/r6-dissect/releases");
                var releaseInfoList = JsonConvert.DeserializeObject<List<ReleaseInfo>>(releaseResponse);
                R6DissectVersion = releaseInfoList?.FirstOrDefault()?.TagName;
                Logger.WriteLine($"r6-dissect latest release is {R6DissectVersion}");

                R6DissectFileName = $"r6-dissect-{R6DissectVersion}-windows-amd64";
                R6DissectZipUrl = $"https://github.com/redraskal/r6-dissect/releases/download/{R6DissectVersion}/{R6DissectFileName}.zip";
                R6DissectMd5Url = $"https://github.com/redraskal/r6-dissect/releases/download/{R6DissectVersion}/{R6DissectFileName}.zip.md5";

                // Download the MD5 hash
                string expectedMd5Hash = await client.GetStringAsync(R6DissectMd5Url);
                expectedMd5Hash = expectedMd5Hash.Trim();

                if (File.Exists(MD5FilePath)) {
                    // Check if the saved MD5 matches the expected MD5
                    string savedMd5Hash = File.ReadAllText(MD5FilePath).Trim();
                    if (savedMd5Hash == expectedMd5Hash) {
                        Logger.WriteLine("r6-dissect is already downloaded. No action needed.");
                        return false;
                    }
                    Logger.WriteLine("MD5 hash does not match. Redownloading the zip file.");
                }

                return true;
            }
        }

        private static async Task DownloadAndExtractR6Dissect() {
            using (HttpClient client = new HttpClient()) {
                // Download r6-dissect.zip
                Logger.WriteLine("Downloading r6-dissect zip file...");
                var response = await client.GetAsync(R6DissectZipUrl);
                response.EnsureSuccessStatusCode();
                using (var fileStream = new FileStream(R6DissectZipPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    await response.Content.CopyToAsync(fileStream);
                }

                // Verify MD5 hash of the downloaded file
                string downloadedFileMd5Hash;
                using (var md5 = MD5.Create()) {
                    using (var stream = File.OpenRead(R6DissectZipPath)) {
                        downloadedFileMd5Hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    }
                }
                Logger.WriteLine("r6-dissect zip downloaded and verified successfully.");

                // Extract the downloaded zip file
                if (Directory.Exists(R6DissectExtractionPath)) {
                    Directory.Delete(R6DissectExtractionPath, true);
                }

                Logger.WriteLine("Extracting r6-dissect zip...");
                ZipFile.ExtractToDirectory(R6DissectZipPath, R6DissectExtractionPath);
                Logger.WriteLine("r6-dissect extracted successfully.");

                // Save the MD5 hash to md5.txt in the extraction directory
                File.WriteAllText(MD5FilePath, downloadedFileMd5Hash);
                Logger.WriteLine("MD5 hash written to md5.txt in the extracted directory.");

                // Delete the zip file
                File.Delete(R6DissectZipPath);
                Logger.WriteLine("r6-dissect.zip deleted after extraction.");
            }
        }

        private static void DeleteOldMatchFiles() {
            var jsonFiles = Directory.GetFiles(TempFolder, "match_*.json");
            foreach (var jsonFile in jsonFiles) {
                File.Delete(jsonFile);
            }
        }

        private static void ProcessMatchData() {
            var jsonFiles = Directory.GetFiles(TempFolder, "match_*.json");
            if (jsonFiles.Length == 0) {
                Logger.WriteLine($"No Rainbow Six match files found for processing.");
                return;
            }

            var allMatchData = new List<MatchData>();
            string recordingProfileID = null;
            Logger.WriteLine($"Processing {jsonFiles.Length} match files");
            foreach (var jsonFile in jsonFiles) {
                var fileName = Path.GetFileName(jsonFile);
                try {
                    Logger.WriteLine($"Processing match file: {fileName}");
                    string jsonData = File.ReadAllText(jsonFile.Replace('\\', '/'));
                    MatchData matchData = JsonConvert.DeserializeObject<MatchData>(jsonData);
                    allMatchData.Add(matchData);

                    Logger.WriteLine($"{matchData.Rounds.Count} rounds in current match");
                    if (recordingProfileID == null && matchData.Rounds.Count != 0) {
                        recordingProfileID = matchData.Rounds.First().RecordingProfileID;
                    }
                }
                catch (Exception ex) {
                    Logger.WriteLine($"Error processing JSON file {fileName}: {ex.Message}");
                }
                finally {
                    try {
                        File.Delete(jsonFile);
                    }
                    catch (Exception ex) {
                        Logger.WriteLine($"Error deleting JSON file {fileName}: {ex.Message}");
                    }
                }
            }

            if (recordingProfileID == null) {
                Logger.WriteLine("Recording profile ID not found.");
                return;
            }

            var allKillTimestamps = new List<DateTime>();
            var allDeathTimestamps = new List<DateTime>();

            foreach (var matchData in allMatchData) {
                var (killTimestamps, deathTimestamps) = matchData.GetKillAndDeathTimestamps(recordingProfileID);
                allKillTimestamps.AddRange(killTimestamps);
                allDeathTimestamps.AddRange(deathTimestamps);
            }

            foreach (var timestamp in allKillTimestamps) {
                BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Kill }, timestamp);
                Logger.WriteLine($"Kill timestamp (epoch): {((DateTimeOffset)timestamp).ToUnixTimeSeconds()}");
            }

            foreach (var timestamp in allDeathTimestamps) {
                BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Death }, timestamp);
                Logger.WriteLine($"Death timestamp (epoch): {((DateTimeOffset)timestamp).ToUnixTimeSeconds()}");
            }

            Logger.WriteLine("Finished processing match data.");
        }

        private static void RunCommand(string command) {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command) {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo)) {
                using (var reader = process.StandardOutput) {
                    string result = reader.ReadToEnd();
                    Logger.WriteLine(result);
                }

                using (var errorReader = process.StandardError) {
                    string errorResult = errorReader.ReadToEnd();
                    if (!string.IsNullOrEmpty(errorResult)) {
                        Logger.WriteLine("Error: " + errorResult);
                    }
                }

                process.WaitForExit();
            }
        }
    }
}
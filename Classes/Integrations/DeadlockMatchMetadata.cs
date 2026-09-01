using Microsoft.Win32;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RePlays.Integrations {
    // Reads a finished Deadlock match's server-authoritative stats out of Steam's local
    // HTTP cache - no Steam login, no third-party web API.
    //
    // When the post-game screen loads, the Deadlock client downloads the match metadata
    // from Valve's replay servers (http://replay{cluster}.valve.net/1422450/{matchId}_{salt}.meta.bz2)
    // and Steam caches the response under <Steam>/appcache/httpcache. The body is a
    // compressed CMsgMatchMetaData protobuf whose match_details holds
    // CMsgMatchMetaDataContents, and its match_info has per-player kills/deaths/assists
    // plus every death with its game-clock time - enough to reconstruct kill/death
    // bookmarks for matchmade games, where console.log logs no kill feed at all.
    //
    // Cache discovery and decoding mirror the deadlock-matches python package and
    // deadlock-api-ingest: scan cache files for the replay URL, find the compressed
    // body by its magic bytes, then walk the protobuf by hand for the handful of
    // fields we need (schema: SteamDatabase/Protobufs deadlock/citadel_gcmessages_common.proto).
    //
    // The body kept its .meta.bz2 filename when Valve switched it from bzip2 to
    // Zstandard in July 2026. Since this code only ever reads entries Steam wrote
    // moments ago - never historical ones - only zstd is decoded; a bzip2 body is
    // detected and reported (rather than silently failing) in case Valve reverts.
    internal static class DeadlockMatchMetadata {
        private static readonly byte[] Bz2Magic = { (byte)'B', (byte)'Z', (byte)'h' };
        private static readonly byte[] ZstdMagic = { 0x28, 0xB5, 0x2F, 0xFD };
        private const long Steam64Base = 76561197960265728;

        private static readonly HttpClient httpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient() {
            var client = new HttpClient {
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RePlays");
            return client;
        }

        // Searches every known Steam httpcache location for this match's .meta cache
        // entry and decodes it. Returns null while the entry hasn't appeared yet (the
        // client only fetches it once the post-game screen loads) - poll and retry.
        public static DeadlockMatchInfo TryLoadFromSteamCache(long matchId, DateTime modifiedAfter) {
            // The salt in the filename is per-match, so match on "{matchId}_<salt>.meta"
            var urlPattern = new Regex($@"/1422450/{matchId}_(\d+)\.meta\.bz2");

            foreach (string cacheDir in CacheDirCandidates()) {
                if (!Directory.Exists(cacheDir)) continue;

                IEnumerable<string> files;
                try {
                    files = Directory.EnumerateFiles(cacheDir, "*", SearchOption.AllDirectories);
                }
                catch (Exception ex) {
                    Logger.WriteLine($"Deadlock metadata: cannot enumerate {cacheDir}: {ex.Message}");
                    continue;
                }

                foreach (string file in files) {
                    try {
                        var info = new FileInfo(file);
                        if (info.LastWriteTime < modifiedAfter || info.Length < 64) continue;

                        // The URL sits in the entry header, near the start of the file
                        string head = ReadHeadAsLatin1(file, 4096);
                        if (!urlPattern.IsMatch(head)) continue;

                        Logger.WriteLine($"Deadlock metadata: found cache entry for match {matchId} at '{file}' ({info.Length} bytes)");
                        return Decode(File.ReadAllBytes(file), matchId);
                    }
                    catch (IOException) {
                        // Steam may still be writing the entry; next poll will get it
                    }
                    catch (Exception ex) {
                        Logger.WriteLine($"Deadlock metadata: failed reading cache file '{file}': {ex.Message}");
                    }
                }
            }

            return null;
        }

        // Fallback for when the metadata never appears in the local Steam http cache -
        // the current game client only writes it there on a manual replay download.
        // The GC hands the game the match's salts automatically every match but never
        // persists them, so this asks deadlock-api.com (a ~200 byte lookup of just
        // those numbers), then downloads and decodes the metadata itself directly
        // from Valve's replay servers, same as the cache path. Returns null when the
        // salts aren't known yet (their backend fetches on demand and answers 503
        // "retry later"); callers retry with backoff.
        public static DeadlockMatchInfo TryLoadFromSaltsApi(long matchId) {
            string saltsJson;
            try {
                using var response = httpClient.GetAsync($"https://api.deadlock-api.com/v1/matches/{matchId}/salts").GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode) {
                    Logger.WriteLine($"Deadlock metadata: salts lookup for match {matchId} returned {(int)response.StatusCode}; will retry later");
                    return null;
                }
                saltsJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex) {
                Logger.WriteLine($"Deadlock metadata: salts lookup for match {matchId} failed: {ex.Message}");
                return null;
            }

            string metadataUrl = null;
            try {
                using var doc = JsonDocument.Parse(saltsJson);
                if (doc.RootElement.TryGetProperty("metadata_url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String) {
                    metadataUrl = urlProp.GetString();
                }
                else if (doc.RootElement.TryGetProperty("cluster_id", out var cluster) &&
                         doc.RootElement.TryGetProperty("metadata_salt", out var salt) &&
                         salt.ValueKind == JsonValueKind.Number) {
                    metadataUrl = $"http://replay{cluster.GetUInt32()}.valve.net/1422450/{matchId}_{salt.GetUInt32()}.meta.bz2";
                }
            }
            catch (Exception ex) {
                Logger.WriteLine($"Deadlock metadata: could not parse salts response for match {matchId}: {ex.Message}");
                return null;
            }
            if (string.IsNullOrEmpty(metadataUrl)) {
                Logger.WriteLine($"Deadlock metadata: salts response for match {matchId} has no metadata url; will retry later");
                return null;
            }

            try {
                byte[] body = httpClient.GetByteArrayAsync(metadataUrl).GetAwaiter().GetResult();
                Logger.WriteLine($"Deadlock metadata: downloaded {body.Length} bytes for match {matchId} from Valve's replay server");
                return Decode(body, matchId);
            }
            catch (Exception ex) {
                Logger.WriteLine($"Deadlock metadata: downloading '{metadataUrl}' failed: {ex.Message}");
                return null;
            }
        }

        // The local player's Steam32 account id, used to find them among the match's
        // 12 players. ActiveProcess\ActiveUser is the account currently logged in to
        // the Steam client; loginusers.vdf is the fallback when that's unavailable.
        public static uint? GetLocalAccountId() {
            try {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam\ActiveProcess");
                if (key?.GetValue("ActiveUser") is int activeUser && activeUser > 0) {
                    return (uint)activeUser;
                }
            }
            catch (Exception ex) {
                Logger.WriteLine($"Deadlock metadata: could not read ActiveUser from registry: {ex.Message}");
            }

            try {
                string steamPath = GetSteamPath();
                if (steamPath != null) {
                    string vdf = Path.Combine(steamPath, "config", "loginusers.vdf");
                    if (File.Exists(vdf)) {
                        // Blocks look like: "76561198012345678" { ... "MostRecent" "1" ... }
                        var blockPattern = new Regex("\"(\\d{17})\"\\s*\\{([^{}]*)\\}");
                        foreach (Match block in blockPattern.Matches(File.ReadAllText(vdf))) {
                            if (Regex.IsMatch(block.Groups[2].Value, "\"MostRecent\"\\s+\"1\"")) {
                                return (uint)(long.Parse(block.Groups[1].Value) - Steam64Base);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logger.WriteLine($"Deadlock metadata: could not read loginusers.vdf: {ex.Message}");
            }

            return null;
        }

        private static string GetSteamPath() {
            try {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key?.GetValue("SteamPath") is string path && !string.IsNullOrEmpty(path)) {
                    return path.Replace('/', Path.DirectorySeparatorChar);
                }
            }
            catch {
                // registry unavailable (non-Windows); fall through to defaults
            }

            string defaultPath = @"C:\Program Files (x86)\Steam";
            return Directory.Exists(defaultPath) ? defaultPath : null;
        }

        private static IEnumerable<string> CacheDirCandidates() {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string steamPath = GetSteamPath();
            if (steamPath != null && seen.Add(steamPath)) {
                yield return Path.Combine(steamPath, "appcache", "httpcache");
            }

            if (seen.Add(@"C:\Program Files (x86)\Steam")) {
                yield return @"C:\Program Files (x86)\Steam\appcache\httpcache";
            }

            // Linux installs (native, then flatpak)
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home)) {
                yield return Path.Combine(home, ".steam", "steam", "appcache", "httpcache");
                yield return Path.Combine(home, ".local", "share", "Steam", "appcache", "httpcache");
                yield return Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam", "appcache", "httpcache");
            }
        }

        private static string ReadHeadAsLatin1(string file, int maxBytes) {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] buffer = new byte[Math.Min(maxBytes, fs.Length)];
            int read = fs.Read(buffer, 0, buffer.Length);
            return Encoding.Latin1.GetString(buffer, 0, read);
        }

        // Cache entry layout: Steam's own header (which contains the URL), then the raw
        // HTTP response body. The body is located by its compression magic rather than
        // by parsing Steam's undocumented header format.
        private static DeadlockMatchInfo Decode(byte[] data, long matchId) {
            int zstdStart = IndexOf(data, ZstdMagic);
            if (zstdStart < 0) {
                throw new InvalidDataException(IndexOf(data, Bz2Magic) >= 0
                    ? "cache entry body looks bzip2-compressed; only Zstandard (used by Valve since July 2026) is supported"
                    : "no Zstandard stream found in cache entry");
            }

            using var output = new MemoryStream();
            using (var input = new MemoryStream(data, zstdStart, data.Length - zstdStart))
            using (var zstd = new ZstdSharp.DecompressionStream(input)) {
                zstd.CopyTo(output);
            }

            var parsed = ParseMatchMetaData(output.ToArray());
            if (parsed.MatchId != 0 && parsed.MatchId != matchId) {
                throw new InvalidDataException($"cache entry contains match {parsed.MatchId}, expected {matchId}");
            }
            return parsed;
        }

        private static int IndexOf(byte[] haystack, byte[] needle) {
            for (int i = 0; i <= haystack.Length - needle.Length; i++) {
                int j = 0;
                while (j < needle.Length && haystack[i + j] == needle[j]) j++;
                if (j == needle.Length) return i;
            }
            return -1;
        }

        // message CMsgMatchMetaData { bytes match_details = 2; uint64 match_id = 3; }
        private static DeadlockMatchInfo ParseMatchMetaData(byte[] buf) {
            var reader = new ProtoReader(buf, 0, buf.Length);
            DeadlockMatchInfo result = null;
            long outerMatchId = 0;

            while (reader.TryReadTag(out int field, out int wire)) {
                if (field == 2 && wire == 2) {
                    var (offset, length) = reader.ReadBytes();
                    result = ParseMetaDataContents(buf, offset, length);
                }
                else if (field == 3 && wire == 0) {
                    outerMatchId = (long)reader.ReadVarint();
                }
                else {
                    reader.SkipField(wire);
                }
            }

            if (result == null) {
                throw new InvalidDataException("CMsgMatchMetaData has no match_details");
            }
            if (result.MatchId == 0) result.MatchId = outerMatchId;
            return result;
        }

        // message CMsgMatchMetaDataContents { MatchInfo match_info = 2; }
        private static DeadlockMatchInfo ParseMetaDataContents(byte[] buf, int offset, int length) {
            var reader = new ProtoReader(buf, offset, length);
            while (reader.TryReadTag(out int field, out int wire)) {
                if (field == 2 && wire == 2) {
                    var (infoOffset, infoLength) = reader.ReadBytes();
                    return ParseMatchInfo(buf, infoOffset, infoLength);
                }
                reader.SkipField(wire);
            }
            throw new InvalidDataException("CMsgMatchMetaDataContents has no match_info");
        }

        // message MatchInfo { uint32 duration_s = 1; ECitadelLobbyTeam winning_team = 3;
        //                     repeated Players players = 4; uint64 match_id = 6; ... }
        private static DeadlockMatchInfo ParseMatchInfo(byte[] buf, int offset, int length) {
            var reader = new ProtoReader(buf, offset, length);
            var result = new DeadlockMatchInfo();

            while (reader.TryReadTag(out int field, out int wire)) {
                switch (field) {
                    case 1 when wire == 0:
                        result.DurationS = (int)reader.ReadVarint();
                        break;
                    case 3 when wire == 0:
                        result.WinningTeam = (int)reader.ReadVarint();
                        break;
                    case 4 when wire == 2: {
                            var (playerOffset, playerLength) = reader.ReadBytes();
                            result.Players.Add(ParsePlayer(buf, playerOffset, playerLength));
                            break;
                        }
                    case 6 when wire == 0:
                        result.MatchId = (long)reader.ReadVarint();
                        break;
                    default:
                        reader.SkipField(wire);
                        break;
                }
            }
            return result;
        }

        // message Players { uint32 account_id = 1; uint32 player_slot = 2;
        //                   repeated Deaths death_details = 3; ECitadelLobbyTeam team = 6;
        //                   uint32 kills = 8; uint32 deaths = 9; uint32 assists = 10;
        //                   uint32 hero_id = 12; ... }
        private static DeadlockMatchPlayer ParsePlayer(byte[] buf, int offset, int length) {
            var reader = new ProtoReader(buf, offset, length);
            var player = new DeadlockMatchPlayer();

            while (reader.TryReadTag(out int field, out int wire)) {
                switch (field) {
                    case 1 when wire == 0:
                        player.AccountId = (uint)reader.ReadVarint();
                        break;
                    case 2 when wire == 0:
                        player.PlayerSlot = (int)reader.ReadVarint();
                        break;
                    case 3 when wire == 2: {
                            var (deathOffset, deathLength) = reader.ReadBytes();
                            player.DeathEvents.Add(ParseDeath(buf, deathOffset, deathLength));
                            break;
                        }
                    case 6 when wire == 0:
                        player.Team = (int)reader.ReadVarint();
                        break;
                    case 8 when wire == 0:
                        player.Kills = (int)reader.ReadVarint();
                        break;
                    case 9 when wire == 0:
                        player.Deaths = (int)reader.ReadVarint();
                        break;
                    case 10 when wire == 0:
                        player.Assists = (int)reader.ReadVarint();
                        break;
                    case 12 when wire == 0:
                        player.HeroId = (int)reader.ReadVarint();
                        break;
                    default:
                        reader.SkipField(wire);
                        break;
                }
            }
            return player;
        }

        // message Deaths { uint32 game_time_s = 1; uint32 killer_player_slot = 9; ... }
        private static DeadlockDeathEvent ParseDeath(byte[] buf, int offset, int length) {
            var reader = new ProtoReader(buf, offset, length);
            var death = new DeadlockDeathEvent();

            while (reader.TryReadTag(out int field, out int wire)) {
                if (field == 1 && wire == 0) {
                    death.GameTimeS = (int)reader.ReadVarint();
                }
                else if (field == 9 && wire == 0) {
                    death.KillerPlayerSlot = (int)reader.ReadVarint();
                }
                else {
                    reader.SkipField(wire);
                }
            }
            return death;
        }

        // Just enough of the protobuf wire format (proto2, so scalar presence is
        // explicit) to avoid taking a Google.Protobuf + codegen dependency for the
        // handful of fields read above.
        private struct ProtoReader {
            private readonly byte[] buf;
            private readonly int end;
            private int pos;

            public ProtoReader(byte[] buf, int offset, int length) {
                this.buf = buf;
                pos = offset;
                end = offset + length;
            }

            public bool TryReadTag(out int fieldNumber, out int wireType) {
                if (pos >= end) {
                    fieldNumber = 0;
                    wireType = 0;
                    return false;
                }
                ulong tag = ReadVarint();
                fieldNumber = (int)(tag >> 3);
                wireType = (int)(tag & 7);
                return true;
            }

            public ulong ReadVarint() {
                ulong value = 0;
                int shift = 0;
                while (pos < end && shift < 64) {
                    byte b = buf[pos++];
                    value |= (ulong)(b & 0x7F) << shift;
                    if ((b & 0x80) == 0) return value;
                    shift += 7;
                }
                throw new InvalidDataException("malformed varint");
            }

            public (int offset, int length) ReadBytes() {
                int length = (int)ReadVarint();
                if (length < 0 || pos + length > end) {
                    throw new InvalidDataException("length-delimited field overruns buffer");
                }
                var segment = (pos, length);
                pos += length;
                return segment;
            }

            public void SkipField(int wireType) {
                switch (wireType) {
                    case 0: ReadVarint(); break;
                    case 1: pos += 8; break;
                    case 2: ReadBytes(); break;
                    case 5: pos += 4; break;
                    default: throw new InvalidDataException($"unsupported wire type {wireType}");
                }
                if (pos > end) throw new InvalidDataException("field overruns buffer");
            }
        }
    }

    internal class DeadlockMatchInfo {
        public long MatchId;
        public int DurationS;
        public int? WinningTeam;
        public List<DeadlockMatchPlayer> Players = new List<DeadlockMatchPlayer>();
    }

    internal class DeadlockMatchPlayer {
        public uint AccountId;
        public int PlayerSlot = -1;
        public int? Team;
        public int HeroId;
        public int Kills;
        public int Deaths;
        public int Assists;
        public List<DeadlockDeathEvent> DeathEvents = new List<DeadlockDeathEvent>();
    }

    internal class DeadlockDeathEvent {
        public int GameTimeS;
        public int KillerPlayerSlot = -1;
    }

    // One saved video still waiting for its match metadata to appear in the Steam
    // http cache (which happens when the user opens the match in Deadlock's match
    // history). Persisted as json in the cfg folder by DeadlockIntegration.
    internal class DeadlockPendingStat {
        public long MatchId { get; set; }
        public string VideoPath { get; set; }
        public DateTime? AnchorStart { get; set; }
        public DateTime? AnchorEnd { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApiAttemptedAt { get; set; } // last salts-api fallback attempt, for backoff
    }
}

using RePlays.Services;
using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RePlays.Integrations {
    // Deadlock does not have GSI (Game State Integration) yet, unlike CS2. The only
    // window into game state is console.log, which is written when the game is
    // launched with the "-condebug" Steam launch option. This is the same approach
    // used by https://github.com/Jelloge/Deadlock-Rich-Presence - regex patterns and
    // state-machine logic below are ported from that project's config.json /
    // console_log.py, tracking game phase (for in-game/not-in-game detection) and
    // the player's current hero. Party/match-mode tracking was stripped out since
    // it's not needed for either of those.
    internal class DeadlockIntegration : Integration {

        private enum GamePhase {
            NotRunning,
            MainMenu,
            Hideout,
            InQueue,
            MatchIntro,
            InMatch,
            PostMatch,
            Spectating
        }

        private class GameState {
            public GamePhase Phase = GamePhase.NotRunning;
            public string MapName;
            public string ServerAddress;
            public bool IsLoopback;
            public DateTime? MatchStartTime;
            public DateTime? QueueStartTime;
            public int? GameStateId;
            public string HeroKey; // internal codename, e.g. "abrams", "wraith"
            public bool IsTransformed; // Silver's wolf form

            // What we actually care about: is the player in a live match (or
            // spectating one from within), as opposed to menu/hideout/queue.
            public bool IsInGame =>
                Phase == GamePhase.MatchIntro ||
                Phase == GamePhase.InMatch ||
                Phase == GamePhase.Spectating;

            public void EnterMainMenu() {
                Phase = GamePhase.MainMenu;
                ClearMatch();
            }

            public void EnterHideout() {
                Phase = GamePhase.Hideout;
                ClearMatch();
                IsLoopback = true;
            }

            public void EnterQueue() {
                Phase = GamePhase.InQueue;
                QueueStartTime = DateTime.Now;
            }

            public void LeaveQueue() {
                QueueStartTime = null;
                EnterHideout();
            }

            public void EnterSpectating() {
                Phase = GamePhase.Spectating;
                HeroKey = null;
                IsTransformed = false;
                MatchStartTime = null;
                QueueStartTime = null;
            }

            public void EnterMatchIntro() {
                Phase = GamePhase.MatchIntro;
                GameStateId = 4;
            }

            public void StartMatch() {
                Phase = GamePhase.InMatch;
                if (MatchStartTime == null) {
                    MatchStartTime = DateTime.Now;
                }
                QueueStartTime = null;
                GameStateId = 5;
            }

            public void EndMatch() {
                Phase = GamePhase.PostMatch;
                MatchStartTime = null;
                GameStateId = 6;
            }

            // No hero-name database here (unlike the reference project's HeroDataStore,
            // which fetches hero list from deadlock-api.com), so this uses the same
            // "no store" fallback it does: strip exactly one trailing "_xxx" segment
            // if present. Handles VMDL variant folder names like "mirage_v2" -> "mirage".
            // Legitimate hero keys are almost always single words after the "hero_"
            // prefix is stripped, so this rarely misfires.
            public void SetHero(string heroKey) {
                string normalized = heroKey;
                int lastUnderscore = normalized.LastIndexOf('_');
                if (lastUnderscore > 0) {
                    normalized = normalized.Substring(0, lastUnderscore);
                }

                if (normalized != HeroKey) {
                    HeroKey = normalized;
                    IsTransformed = false; // reset form on hero change
                }
            }

            public void ConnectToServer(string address) {
                ServerAddress = address;
                IsLoopback = address.IndexOf("loopback", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            public void ClearMatch() {
                MatchStartTime = null;
                ServerAddress = null;
                MapName = null;
                GameStateId = null;
                IsTransformed = false;
            }

            public void Reset() {
                ClearMatch();
                Phase = GamePhase.NotRunning;
                HeroKey = null;
                QueueStartTime = null;
                IsLoopback = false;
            }
        }

        // Ported verbatim from Jelloge/Deadlock-Rich-Presence's config.json "log_patterns".
        // (Party-only patterns like "bot_init", "player_info", "local_account_id", and
        // "hideout_lobby_state" were dropped - they only affect match-mode classification
        // and party size, not in-game/not-in-game state or hero.)
        private static readonly Dictionary<string, Regex> Patterns = new Dictionary<string, Regex> {
            ["change_game_state"] = new Regex(@"ChangeGameState:\s+(\w+)\s+\((\d+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["server_connect"] = new Regex(@"\[Client\] CL:\s+Connected to '([^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["loaded_hero"] = new Regex(@"\[Server\] Loaded hero \d+/(hero_\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["client_hero_vmdl"] = new Regex(@"VMDL Camera Pose Success!.*models/heroes(?:_wip|_staging)?/(\w+)/", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["silver_wolf_form_on"] = new Regex(@"werewolf_transform\.vmdl", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["silver_wolf_form_off"] = new Regex(@"werewolf\.vmdl", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["server_disconnect"] = new Regex(@"\[Client\] Disconnecting from server:\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["server_shutdown"] = new Regex(@"\[Server\] SV:\s+Server shutting down:\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["map_created_physics"] = new Regex(@"\[Client\] Created physics for\s+([^\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["map_info"] = new Regex("\\[Client\\] Map:\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["host_activate"] = new Regex(@"\[HostStateManager\] Host activate:.*\(([^)]+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["app_shutdown"] = new Regex(@"Dispatching EventAppShutdown_t", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["source2_shutdown"] = new Regex(@"Source2Shutdown", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["precaching_heroes"] = new Regex(@"Precaching (\d+) heroes in CCitadelGameRules", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["loop_mode_menu"] = new Regex(@"LoopMode:\s*menu", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["lobby_created"] = new Regex(@"Lobby\s+\d+\s+for\s+Match\s+\d+\s+created", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["lobby_destroyed"] = new Regex(@"Lobby\s+\d+\s+for\s+Match\s+\d+\s+destroyed", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["spectate_broadcast"] = new Regex(@"Playing Broadcast", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["mm_start"] = new Regex(@"\[GCClient\] Send msg 9010 \(k_EMsgClientToGCStartMatchmaking\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            ["mm_stop"] = new Regex(@"\[GCClient\] Send msg 9012 \(k_EMsgClientToGCStopMatchmaking\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            // Death notice, e.g. "Killer: hero_synth, Dying: hero_atlas, Number of Assisters: 1".
            // Not from the reference project - found by mining a real console.log. CAVEAT: this
            // line is only written when the game runs its embedded local server (sandbox,
            // tutorial, hideout). A real matchmade game runs on a dedicated server and the
            // client logs no kill feed at all, so KDA bookmarks only work in local modes.
            ["death_notice"] = new Regex(@"Killer:\s+(\S+?),\s+Dying:\s+(\S+?),\s+Number of Assisters:\s+(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };

        // Log lines are prefixed with e.g. "08/30 16:20:07 " (no year).
        private static readonly Regex LineTimestampPattern = new Regex(@"^(\d{2})/(\d{2})\s+(\d{2}):(\d{2}):(\d{2})\s", RegexOptions.Compiled);

        private static readonly HashSet<string> HideoutMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "dl_hideout"
        };

        // Known match maps that aren't always reliably caught by change_game_state
        // alone (mirrors the "map_to_mode" keys from the reference project's config).
        private static readonly HashSet<string> KnownMatchMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "street_test", "street_test_bridge", "new_player_basics", "dl_midtown"
        };

        // Matches "resync_max_bytes" in the reference project's config.json. 100 KB (its
        // hardcoded fallback) is not enough - Deadlock writes several MB per match, so a
        // small window can cut off the map_info/lobby_created lines that establish the
        // current phase when we attach to an already-running game.
        private const long ResyncMaxBytes = 10 * 1024 * 1024;
        private const int PollIntervalMs = 1000;

        // How long after a match ends before the recording is split, so the post-game
        // scoreboard (and a bit of the aftermath) stays in the match's video.
        private const int MatchEndRestartDelayMs = 2 * 60 * 1000;

        // Logs which pattern actually handled each line and every phase/hero transition.
        // Cheap enough to leave on while confirming things work; turn off once happy.
        private const bool VerboseLogging = false;

        // Dumps every single line read from console.log. Only for debugging the reader
        // itself (file paths, rotation, encoding) - a live match writes thousands of
        // lines a minute and this will flood the log file.
        private const bool VerboseRawLines = false;

        private readonly GameState state = new GameState();
        private string logPath;
        private FileStream fileStream;
        private StreamReader reader;
        private long lastSize;
        private bool hideoutLoaded;
        private bool heroWindowOpen = true;
        private CancellationTokenSource cts;
        private Task watchTask;
        private string lastHandledPattern; // set by MatchPattern, for verbose logging only
        private bool wasInGame; // last IsInGame value seen by OnStateChanged, for match-end detection
        private CancellationTokenSource restartDelayCts; // pending post-match recording split

        // Per-video stats written into the session's .metadata on recorder stop (see
        // LibObsRecorder.StopRecording), mirroring LeagueOfLegendsIntegration. Kept
        // separate from GameState because Reset() (fired by the game-exit log lines,
        // which usually arrive before the recorder stops) clears HeroKey.
        private string lastHeroKey;
        private int killCount;
        private int deathCount;

        private string GetConsoleLogPath() {
            const string searchPath = "\\game\\bin\\win64\\deadlock.exe";
            const string replacePath = "\\game\\citadel\\console.log";

            var currentSession = RecordingService.GetCurrentSession();
            if (currentSession == null || string.IsNullOrEmpty(currentSession.Exe)) {
                throw new Exception("Could not get Deadlock path, Session path: " + currentSession?.Exe);
            }

            string exePath = currentSession.Exe;
            Logger.WriteLine($"Deadlock integration: exe path is '{exePath}'");

            // Case-insensitive replace - plain string.Replace is case-sensitive and will
            // silently no-op (returning exePath unchanged) if Windows gives us different
            // casing than expected, e.g. "\Game\Bin\Win64\" instead of "\game\bin\win64\".
            string logPath = Regex.Replace(exePath, Regex.Escape(searchPath), replacePath.Replace("$", "$$"), RegexOptions.IgnoreCase);

            if (string.Equals(logPath, exePath, StringComparison.Ordinal)) {
                Logger.WriteLine(
                    $"Deadlock integration: could not derive console.log path from exe path " +
                    $"(expected it to end with '{searchPath}'). Falling back to directory-relative lookup.");

                // Fallback: exe is .../game/bin/win64/project8.exe -> walk up to .../game
                // and append citadel/console.log, regardless of exact exe filename/casing.
                string win64Dir = Path.GetDirectoryName(exePath);
                string gameDir = win64Dir != null ? Directory.GetParent(win64Dir)?.Parent?.FullName : null;
                if (gameDir != null) {
                    logPath = Path.Combine(gameDir, "citadel", "console.log");
                }
                else {
                    throw new Exception($"Could not resolve Deadlock's console.log path from exe path: {exePath}");
                }
            }

            Logger.WriteLine($"Deadlock integration: resolved console.log path to '{logPath}'");
            return logPath;
        }

        public override async Task Start() {
            Logger.WriteLine("Starting Deadlock integration");

            try {
                logPath = GetConsoleLogPath();
            }
            catch (Exception ex) {
                Logger.WriteLine($"Deadlock integration could not resolve console.log path: {ex.Message}");
                return;
            }

            if (!File.Exists(logPath)) {
                Logger.WriteLine(
                    $"Deadlock console.log not found at {logPath}. Deadlock does not support GSI yet - " +
                    "add \"-condebug\" to Deadlock's Steam launch options so this file gets written.");
            }

            state.EnterMainMenu();
            hideoutLoaded = false;
            heroWindowOpen = true;
            wasInGame = false;
            cts = new CancellationTokenSource();
            watchTask = Task.Run(() => WatchLoop(cts.Token));

            await Task.CompletedTask;
        }

        private void WatchLoop(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                try {
                    if (!File.Exists(logPath)) {
                        Thread.Sleep(PollIntervalMs * 3);
                        continue;
                    }

                    if (fileStream == null || IsFileRotated()) {
                        OpenLog();
                        Resync();
                    }

                    string line;
                    bool changed = false;
                    int linesRead = 0;
                    while ((line = reader.ReadLine()) != null) {
                        linesRead++;
                        line = line.Trim();
                        if (line.Length > 0) {
                            changed = ProcessLine(line, isResync: false) || changed;
                        }
                    }
                    long newLength = fileStream.Length;

                    if (VerboseRawLines && linesRead > 0) {
                        Logger.WriteLine($"[Deadlock poll] readLines={linesRead} fileLength={newLength} lastKnown={lastSize}");
                    }

                    lastSize = newLength;

                    if (changed) {
                        OnStateChanged();
                    }
                }
                catch (Exception ex) {
                    Logger.WriteLine($"Deadlock integration watch loop error: {ex.Message}");
                }

                Thread.Sleep(PollIntervalMs);
            }
        }

        private bool IsFileRotated() {
            try {
                var info = new FileInfo(logPath);
                return info.Length < lastSize;
            }
            catch {
                return true;
            }
        }

        private void OpenLog() {
            CloseLog();
            fileStream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fileStream.Seek(0, SeekOrigin.End);
            lastSize = fileStream.Position;
            reader = new StreamReader(fileStream, Encoding.UTF8);
            Logger.WriteLine($"Opened Deadlock console.log at byte {lastSize}");
        }

        private void CloseLog() {
            reader?.Dispose();
            fileStream?.Dispose();
            reader = null;
            fileStream = null;
        }

        // Reads the tail of console.log (up to ResyncMaxBytes) so state is correct
        // even if we start watching mid-session (e.g. player already in a match).
        private void Resync() {
            try {
                var info = new FileInfo(logPath);
                long readStart = Math.Max(0, info.Length - ResyncMaxBytes);

                if (VerboseLogging) {
                    Logger.WriteLine($"[Deadlock resync] ===== BEGIN backlog replay ({info.Length - readStart} bytes) - " +
                        "lines tagged 'resync' below are historical, not live =====");
                }

                using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.UTF8)) {
                    if (readStart > 0) {
                        fs.Seek(readStart, SeekOrigin.Begin);
                        sr.ReadLine(); // discard partial line
                    }

                    string line;
                    while ((line = sr.ReadLine()) != null) {
                        line = line.Trim();
                        if (line.Length > 0) {
                            ProcessLine(line, isResync: true);
                        }
                    }
                }

                if (VerboseLogging) {
                    Logger.WriteLine("[Deadlock resync] ===== END backlog replay - everything from here on is live =====");
                }

                lastSize = info.Length;
                OnStateChanged();
            }
            catch (Exception ex) {
                Logger.WriteLine($"Deadlock integration resync error: {ex.Message}");
            }
        }

        // Returns true if this line caused a phase change. Order mirrors the
        // elif-chain in the reference project's LogWatcher._process_line.
        private bool ProcessLine(string line, bool isResync = false) {
            if (VerboseRawLines) {
                Logger.WriteLine($"[Deadlock raw:{(isResync ? "resync" : "live")}] {line}");
            }

            var oldPhase = state.Phase;
            var oldHero = state.HeroKey;
            var oldTransformed = state.IsTransformed;
            string mapLower = state.MapName != null ? state.MapName.ToLowerInvariant() : "";
            bool inHideoutMap = HideoutMaps.Contains(mapLower);

            lastHandledPattern = null;
            Match m;

            if ((m = MatchPattern("death_notice", line)) != null) {
                if (!isResync) {
                    HandleDeathNotice(m, line);
                }
            }
            else if ((m = MatchPattern("map_info", line)) != null) {
                ApplyMap(m.Groups[1].Value);
            }
            else if ((m = MatchPattern("map_created_physics", line)) != null) {
                ApplyMap(m.Groups[1].Value);
            }
            else if (MatchPattern("mm_start", line) != null) {
                if (state.Phase == GamePhase.Hideout || state.Phase == GamePhase.MainMenu) {
                    state.EnterQueue();
                }
            }
            else if (MatchPattern("mm_stop", line) != null) {
                if (state.Phase == GamePhase.InQueue) {
                    state.LeaveQueue();
                }
            }
            else if (MatchPattern("lobby_created", line) != null) {
                state.MatchStartTime = DateTime.Now;
                state.QueueStartTime = null;
                PrepareMatchHeroTracking();
                if (state.Phase == GamePhase.MainMenu || state.Phase == GamePhase.Hideout || state.Phase == GamePhase.InQueue) {
                    state.EnterMatchIntro();
                }
            }
            else if (MatchPattern("lobby_destroyed", line) != null) {
                state.EndMatch();
            }
            else if (MatchPattern("spectate_broadcast", line) != null) {
                state.EnterSpectating();
                hideoutLoaded = false;
            }
            else if ((m = MatchPattern("server_connect", line)) != null) {
                string addr = m.Groups[1].Value;
                state.ConnectToServer(addr);
                bool isRealServer = addr.IndexOf("loopback", StringComparison.OrdinalIgnoreCase) < 0;
                bool wasInQueue = state.Phase == GamePhase.InQueue;

                if (isRealServer) {
                    PrepareMatchHeroTracking();
                }
                if (isRealServer && (state.Phase == GamePhase.MainMenu || state.Phase == GamePhase.Hideout || state.Phase == GamePhase.InQueue)) {
                    state.EnterMatchIntro();
                }
                if (wasInQueue && isRealServer) {
                    state.QueueStartTime = null;
                }
            }
            else if ((m = MatchPattern("loaded_hero", line)) != null) {
                // Skip during the initial hideout load - hero window isn't open yet,
                // this just avoids a spurious hero pop-in as the hideout loads.
                bool isHideout = state.Phase == GamePhase.Hideout;
                if (!(isHideout && !hideoutLoaded)) {
                    ApplyHeroSignal(m.Groups[1].Value);
                }
            }
            else if ((m = MatchPattern("client_hero_vmdl", line)) != null) {
                // Also handles Silver's wolf-form swap (same VMDL line, different model path)
                string heroNorm = m.Groups[1].Value.ToLowerInvariant();
                ApplyHeroSignal(heroNorm);
                if (state.HeroKey == "werewolf" && heroNorm == "werewolf") {
                    state.IsTransformed = line.ToLowerInvariant().Contains("werewolf_transform");
                }
            }
            else if (MatchPattern("silver_wolf_form_on", line) != null) {
                state.IsTransformed = true;
            }
            else if (MatchPattern("silver_wolf_form_off", line) != null) {
                state.IsTransformed = false;
            }
            else if ((m = MatchPattern("server_disconnect", line)) != null) {
                string reason = m.Groups[1].Value;
                if (reason.IndexOf("EXITING", StringComparison.OrdinalIgnoreCase) >= 0) {
                    OpenHeroWindow();
                    state.Reset();
                }
                else if (reason.IndexOf("LOOPDEACTIVATE", StringComparison.OrdinalIgnoreCase) >= 0) {
                    // no-op, matches upstream
                }
                else if (state.Phase == GamePhase.InMatch || state.Phase == GamePhase.MatchIntro || state.Phase == GamePhase.Spectating) {
                    state.EndMatch();
                }
            }
            else if (MatchPattern("loop_mode_menu", line) != null) {
                if (state.Phase == GamePhase.InMatch || state.Phase == GamePhase.MatchIntro || state.Phase == GamePhase.Spectating) {
                    state.EndMatch();
                }
            }
            else if ((m = MatchPattern("change_game_state", line)) != null) {
                if (state.Phase != GamePhase.Spectating && !inHideoutMap) {
                    string stateName = m.Groups[1].Value.ToLowerInvariant();
                    int stateId = int.Parse(m.Groups[2].Value);
                    state.GameStateId = stateId;

                    if (!hideoutLoaded) {
                        if (stateName == "matchintro" || stateId == 4) {
                            state.EnterMatchIntro();
                        }
                        else if (stateName == "gameinprogress" || stateName == "inprogress" || stateId == 7) {
                            state.StartMatch();
                        }
                        else if (stateName == "postgame" || stateId == 6) {
                            state.EndMatch();
                        }
                    }
                }
            }
            else if ((m = MatchPattern("host_activate", line)) != null) {
                string mapName = m.Groups[1].Value.ToLowerInvariant().Trim();
                if (HideoutMaps.Contains(mapName)) {
                    hideoutLoaded = true;
                }
            }
            else if ((m = MatchPattern("server_shutdown", line)) != null) {
                string reason = m.Groups[1].Value;
                if (reason.IndexOf("EXITING", StringComparison.OrdinalIgnoreCase) >= 0) {
                    OpenHeroWindow();
                    state.Reset();
                }
            }
            else if (MatchPattern("app_shutdown", line) != null || MatchPattern("source2_shutdown", line) != null) {
                OpenHeroWindow();
                state.Reset();
            }
            else if ((m = MatchPattern("precaching_heroes", line)) != null) {
                if (int.Parse(m.Groups[1].Value) > 0) {
                    hideoutLoaded = false;
                }
            }

            if (state.HeroKey != null) {
                lastHeroKey = state.HeroKey;
            }

            bool changed = state.Phase != oldPhase || state.HeroKey != oldHero || state.IsTransformed != oldTransformed;

            if (VerboseLogging && lastHandledPattern != null) {
                string tag = isResync ? "resync" : "live";
                string transition = changed
                    ? $" -> {oldPhase}/{oldHero ?? "-"} => {state.Phase}/{state.HeroKey ?? "-"}"
                    : "";
                Logger.WriteLine($"[Deadlock {tag}:{lastHandledPattern}]{transition} {line}");
            }

            return changed;
        }

        private void ApplyMap(string mapName) {
            if (state.Phase == GamePhase.Spectating) {
                return;
            }

            mapName = mapName.ToLowerInvariant().Trim();
            if (string.IsNullOrEmpty(mapName) || mapName == "<empty>") {
                return;
            }

            state.MapName = mapName;

            if (HideoutMaps.Contains(mapName)) {
                state.EnterHideout();
                state.MapName = mapName;
                OpenHeroWindow();
                hideoutLoaded = false;
                return;
            }

            if (KnownMatchMaps.Contains(mapName)) {
                state.Phase = GamePhase.InMatch;
                PrepareMatchHeroTracking();
                hideoutLoaded = false;
            }
        }

        // Clear hero and reopen the window so the first in-match hero signal is
        // accepted (hero selection screen changes it, Street Brawl assigns randomly).
        private void PrepareMatchHeroTracking() {
            state.HeroKey = null;
            state.IsTransformed = false;
            heroWindowOpen = true;
        }

        private void OpenHeroWindow() {
            heroWindowOpen = true;
        }

        private void CloseHeroWindow() {
            heroWindowOpen = false;
        }

        // NOTE: unlike the reference project, this doesn't track match_mode, so it
        // can't detect Sandbox/Hero Labs to allow free hero re-swapping there like
        // upstream does. Once a hero locks in during MatchIntro/InMatch here, it
        // stays locked until the next match. Fine for "was the player playing hero X"
        // in a normal/ranked/bot match; if you record Sandbox sessions and want hero
        // swaps to update live, this is the spot to add that exception back in.
        private void ApplyHeroSignal(string heroKey) {
            string heroNorm = heroKey.ToLowerInvariant();
            if (heroNorm.StartsWith("hero_")) {
                heroNorm = heroNorm.Substring(5);
            }

            if (state.Phase == GamePhase.Spectating) {
                return;
            }

            if (state.Phase == GamePhase.MatchIntro || state.Phase == GamePhase.InMatch) {
                if (state.HeroKey != null && heroNorm != state.HeroKey) {
                    return;
                }
                if (state.HeroKey == null && !heroWindowOpen) {
                    return;
                }
            }
            else if (state.Phase == GamePhase.MainMenu || state.Phase == GamePhase.PostMatch) {
                return;
            }

            state.SetHero(heroNorm);

            if (state.Phase == GamePhase.MatchIntro || state.Phase == GamePhase.InMatch) {
                CloseHeroWindow();
            }
        }

        // Kill/death bookmarks from local-server death notices. Assist bookmarks are not
        // possible: the death notice only carries a count ("Number of Assisters: 1"), never
        // who assisted, and no other console.log line attributes assists to a player.
        private void HandleDeathNotice(Match m, string line) {
            // Only bookmark real gameplay. In the hideout the NPC heroes spar with each
            // other and generate death notices we don't care about; while spectating,
            // HeroKey is null so nothing would match anyway.
            if (state.Phase != GamePhase.InMatch && state.Phase != GamePhase.MatchIntro) {
                return;
            }
            if (state.HeroKey == null || !RecordingService.IsRecording) {
                return;
            }

            string killer = NormalizeHeroKey(m.Groups[1].Value);
            string dying = NormalizeHeroKey(m.Groups[2].Value);
            DateTime? timestamp = ParseLineTimestamp(line);

            if (dying == state.HeroKey) {
                Logger.WriteLine($"Deadlock death: killed by {killer}");
                deathCount++;
                BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Death }, timestamp);
            }
            else if (killer == state.HeroKey) {
                Logger.WriteLine($"Deadlock kill: killed {dying}");
                killCount++;
                BookmarkService.AddBookmark(new Bookmark { type = Bookmark.BookmarkType.Kill }, timestamp);
            }
        }

        private static string NormalizeHeroKey(string heroKey) {
            string norm = heroKey.ToLowerInvariant().TrimEnd(',');
            return norm.StartsWith("hero_") ? norm.Substring(5) : norm;
        }

        // Log lines carry "MM/dd HH:mm:ss" (no year). More accurate than DateTime.Now when
        // the 1-second poll delivers a burst of lines at once.
        private static DateTime? ParseLineTimestamp(string line) {
            Match m = LineTimestampPattern.Match(line);
            if (!m.Success) {
                return null;
            }

            try {
                var now = DateTime.Now;
                var parsed = new DateTime(now.Year,
                    int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value),
                    int.Parse(m.Groups[3].Value), int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value));
                // Dec 31 -> Jan 1 rollover: a "12/31" line read just after midnight
                // would otherwise be dated a year in the future.
                if (parsed > now.AddDays(1)) {
                    parsed = parsed.AddYears(-1);
                }
                return parsed;
            }
            catch {
                return null;
            }
        }

        // IMPORTANT: must return null when the pattern does not match. Regex.Match never
        // returns null in .NET - it returns a Match with Success == false - so returning it
        // directly makes every `if ((m = MatchPattern(...)) != null)` in ProcessLine's
        // else-if chain true, and the first branch swallows every line.
        private Match MatchPattern(string patternName, string line) {
            if (!Patterns.TryGetValue(patternName, out var pattern)) {
                return null;
            }

            Match match = pattern.Match(line);
            if (!match.Success) {
                return null;
            }

            lastHandledPattern = patternName;
            return match;
        }

        private void OnStateChanged() {
            Logger.WriteLine($"Deadlock state: {state.Phase} | Hero: {state.HeroKey ?? "-"} | Map: {state.MapName ?? "-"} | InGame: {state.IsInGame}");

            // Split the session video when a match ends, so each match gets its own file.
            // Guarded to the PostMatch transition specifically: on game exit (NotRunning)
            // DetectionService stops the recording itself, and restarting there would
            // create a new session file for a dying process. The split is delayed by
            // MatchEndRestartDelayMs so the post-game scoreboard stays in the match video.
            bool isInGame = state.IsInGame;
            if (wasInGame && !isInGame && state.Phase == GamePhase.PostMatch && RecordingService.IsRecording) {
                ScheduleDelayedRestart();
            }
            else if (!wasInGame && isInGame && restartDelayCts != null) {
                // A new match is starting before the post-match buffer elapsed. Split
                // right now so the cut still lands between the two matches instead of
                // two minutes into this one.
                CancelDelayedRestart();
                if (RecordingService.IsRecording) {
                    Logger.WriteLine("Deadlock new match starting with a split pending, restarting recording now");
                    RecordingService.RestartRecording();
                }
            }
            wasInGame = isInGame;
        }

        private void ScheduleDelayedRestart() {
            CancelDelayedRestart();
            var cts = new CancellationTokenSource();
            restartDelayCts = cts;
            Logger.WriteLine($"Deadlock match ended, splitting the recording in {MatchEndRestartDelayMs / 1000}s");

            Task.Run(async () => {
                try {
                    await Task.Delay(MatchEndRestartDelayMs, cts.Token);
                }
                catch (TaskCanceledException) {
                    return;
                }

                if (restartDelayCts == cts) {
                    restartDelayCts = null;
                }
                // Recording may have stopped during the buffer (game closed, manual stop);
                // RestartRecording would refuse anyway, but don't even try.
                if (RecordingService.IsRecording) {
                    Logger.WriteLine("Deadlock post-match buffer elapsed, restarting recording to split the video");
                    RecordingService.RestartRecording();
                }
            });
        }

        private void CancelDelayedRestart() {
            restartDelayCts?.Cancel();
            restartDelayCts = null;
        }

        // Called by LibObsRecorder.StopRecording (same hook as LeagueOfLegendsIntegration)
        // to stamp the finished video's .metadata with the hero played and, for
        // local-server modes where death notices exist, the kill/death counts. The
        // "champion" field holds the Deadlock hero codename (e.g. "atlas"); the client
        // resolves it to an icon via assets.deadlock-api.com.
        public void UpdateMetadataWithStats(string videoPath) {
            if (lastHeroKey == null) return;

            string thumbsDir = Path.Combine(Path.GetDirectoryName(videoPath), ".thumbs/");
            string metadataPath = Path.Combine(thumbsDir, Path.GetFileNameWithoutExtension(videoPath) + ".metadata");
            if (File.Exists(metadataPath)) {
                VideoMetadata metadata = JsonSerializer.Deserialize<VideoMetadata>(File.ReadAllText(metadataPath));
                metadata.champion = lastHeroKey;
                metadata.kills = killCount;
                metadata.deaths = deathCount;
                File.WriteAllText(metadataPath, JsonSerializer.Serialize<VideoMetadata>(metadata));
            }
        }

        public override Task Shutdown() {
            Logger.WriteLine("Shutting down Deadlock integration");
            try {
                CancelDelayedRestart();
                cts?.Cancel();
                watchTask?.Wait(2000);
            }
            catch (Exception ex) {
                Logger.WriteLine($"{ex.Message}");
            }
            finally {
                CloseLog();
            }

            return Task.CompletedTask;
        }
    }
}
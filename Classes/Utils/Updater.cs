using RePlays.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Velopack;
using Velopack.Locators;
using Velopack.Logging;
using Velopack.Sources;
using static RePlays.Utils.Functions;

namespace RePlays.Utils {
    internal sealed class Updater {
        public static string currentVersion = "?";
        public static string latestVersion = "Offline";
        public static bool applyingUpdate { get; internal set; }
        // the manager of the most recent update check, kept so a restart can install the update it downloaded
        static UpdateManager manager;

        static UpdateManager CreateManager() {
            // for testing a build before it is released: a folder holding the output of a release
            // build (releases.win.json and the packages) can be used as the update feed instead
            var localFeed = Environment.GetEnvironmentVariable("REPLAYS_UPDATE_FEED");
            if (!string.IsNullOrWhiteSpace(localFeed)) {
                Logger.WriteLine($"Using local update feed {localFeed}");
                return new UpdateManager(localFeed);
            }
            // releases are read through the github api. every recent release that carries a
            // releases.win.json feed is merged into one list (prereleases included on the nightly
            // channel), so the highest version across the stable and nightly releases wins
            bool nightly = SettingsService.Settings.generalSettings.updateChannel != "Stable";
            return new UpdateManager(new GithubSource("https://github.com/lulzsun/RePlays", null, nightly));
        }

        // true while running from an install that squirrel laid out (an app-x.y.z folder instead of
        // current). such an install is migrated to the velopack layout by "Update.exe start", which
        // is what the launcher stub and shortcuts run, so this only lasts until the next relaunch
        public static bool IsSquirrelLayout() {
            var folder = Path.GetFileName(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
            return folder.StartsWith("app-", StringComparison.OrdinalIgnoreCase);
        }

        public static async void CheckForUpdates(bool forceUpdate = false) {
            if (applyingUpdate) {
                Logger.WriteLine($"Currently in the middle of applying an update. Cannot check for updates.");
                return;
            }
            try {
                if (forceUpdate) WebMessage.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)40, (long)100);

                manager = CreateManager();
                if (!manager.IsInstalled) {
                    // running from a build folder rather than an installed copy, there is nothing to update
                    Logger.WriteLine("RePlays is not installed, skipping the update check.");
                    if (forceUpdate) WebMessage.DestroyToast("CheckUpdateProgress");
                    return;
                }
                currentVersion = manager.CurrentVersion.ToString();
                Logger.Version = $"[v{currentVersion}]";

                if (forceUpdate) WebMessage.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)70, (long)100);

                var updateInfo = await manager.CheckForUpdatesAsync();
                if (forceUpdate) {
                    WebMessage.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)100, (long)100);
                    await Task.Delay(500);
                    WebMessage.DestroyToast("CheckUpdateProgress");
                }
                // null means there is nothing newer than the installed version
                latestVersion = updateInfo?.TargetFullRelease.Version.ToString() ?? currentVersion;
                SettingsService.SaveSettings();
                WebMessage.SendMessage(GetUserSettings());
                if (SettingsService.Settings.generalSettings.update == "none") return;

                if (updateInfo == null) {
                    Logger.WriteLine($"Found no updates higher than current version {currentVersion}");
                    return;
                }
                if (SettingsService.Settings.generalSettings.update != "automatic" && !forceUpdate) {
                    WebMessage.DisplayToast("ManualUpdate", "New version available!", "Update", "info");
                    return;
                }

                Logger.WriteLine($"New version found! Preparing to download version {updateInfo.TargetFullRelease.Version} from {currentVersion}");
                WebMessage.DestroyToast("ManualUpdate");
                applyingUpdate = true;
                await manager.DownloadUpdatesAsync(updateInfo, (progressValue) => {
                    try {
                        WebMessage.DisplayToast("UpdateProgress", "Downloading update", "Updating", "none", (long)progressValue, (long)100);
                    }
                    catch (Exception exception) {
                        // reporting progress must never abort the update itself, otherwise the
                        // release gets downloaded but the user is never asked to restart
                        Logger.WriteLine($"Failed to display update progress: {exception.Message}");
                    }
                });
                WebMessage.DestroyToast("UpdateProgress");
                applyingUpdate = false;
                // velopack installs a downloaded package while RePlays is not running, so the
                // update only takes effect once the user restarts (see Restart below)
                Logger.WriteLine($"Update to version {updateInfo.TargetFullRelease.Version} downloaded, it will be installed on the next restart");
                WebMessage.DisplayModal("New update downloaded! Click Confirm to restart and complete the update.", "Update", "update");
            }
            catch (System.Exception exception) {
                Logger.WriteLine("Error: Issue fetching update releases: " + exception.ToString());
                // otherwise the progress toast is stuck on screen (and replayed on every
                // interface reload) with no way for the user to dismiss it
                WebMessage.DestroyToast("UpdateProgress");
                if (forceUpdate) {
                    WebMessage.DestroyToast("CheckUpdateProgress");
                    WebMessage.DisplayModal("Failed to check for update. More information written to logs.", "Error", "warning");
                }
            }
            applyingUpdate = false;
        }

        // restarts RePlays, installing a downloaded update on the way if there is one waiting
        public static void Restart() {
            try {
                manager ??= CreateManager();
                if (manager.IsInstalled && IsSquirrelLayout()) {
                    // "Update.exe start" performs the one time migration to the velopack layout,
                    // installs whatever full package is waiting in the packages folder (a newer
                    // one if an update was downloaded) and then launches the migrated RePlays
                    Logger.WriteLine("Restarting RePlays through Update.exe to migrate the install");
                    Process.Start(new ProcessStartInfo {
                        FileName = VelopackLocator.Current.UpdateExePath,
                        Arguments = $"start --waitPid {Environment.ProcessId}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                else if (manager.IsInstalled && manager.IsUpdatePendingRestart) {
                    Logger.WriteLine("Restarting to install the downloaded update");
                    // Update.exe waits for this process to exit, installs the update and relaunches RePlays
                    manager.WaitExitThenApplyUpdates(manager.UpdatePendingRestart);
                }
                else {
                    // plain relaunch of this executable, delayed a moment so the single instance
                    // mutex of this process is gone by the time the new one starts
                    Process.Start(new ProcessStartInfo {
                        FileName = "cmd.exe",
                        Arguments = $"/C timeout /t 1 & start \"\" \"{Environment.ProcessPath}\"",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                }
            }
            catch (Exception exception) {
                // better to keep the current instance running than to exit without a replacement
                Logger.WriteLine("Error: Failed to restart RePlays: " + exception.ToString());
                return;
            }
            Process.GetCurrentProcess().Kill(); // this is not a clean exit, need to look into why we can't cleanly exit
        }

        // finishes cleaning up an install that was migrated from clowd.squirrel, once RePlays
        // runs from the current folder of the velopack layout:
        // - squirrel placed a launcher stub for every executable in the package next to Update.exe
        //   (issue #192). nothing but Update.exe and the RePlays.exe launcher belongs in the root of
        //   a velopack install, so every other executable there is a leftover stub
        // - the migration deletes the old app-x.y.z folders, but the one of the version that was
        //   running can still be locked by its exiting child processes at that moment, so that
        //   is retried here on every launch until it is gone
        public static void RemoveSquirrelLeftovers() {
            try {
                var root = VelopackLocator.Current.RootAppDir;
                if (root == null) return;
                var folder = Path.GetFileName(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
                if (!folder.Equals("current", StringComparison.OrdinalIgnoreCase)) return;
                foreach (var file in Directory.EnumerateFiles(root, "*.exe")) {
                    var name = Path.GetFileName(file);
                    if (name.Equals("Update.exe", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.Equals("RePlays.exe", StringComparison.OrdinalIgnoreCase)) continue;
                    File.Delete(file);
                    Logger.WriteLine($"Removed leftover squirrel launcher stub: {name}");
                }
                foreach (var directory in Directory.EnumerateDirectories(root, "app-*")) {
                    try {
                        Directory.Delete(directory, true);
                        Logger.WriteLine($"Removed leftover squirrel version folder: {Path.GetFileName(directory)}");
                    }
                    catch (Exception exception) {
                        Logger.WriteLine($"Could not remove leftover squirrel version folder {Path.GetFileName(directory)} yet: {exception.Message}");
                    }
                }
            }
            catch (Exception exception) {
                Logger.WriteLine("Failed to clean up squirrel leftovers: " + exception.Message);
            }
        }
    }

    // forwards velopack's own log output into the RePlays log, so install and update problems
    // show up in the log users already send along with their reports
    internal sealed class VelopackLogger : IVelopackLogger {
        public void Log(VelopackLogLevel logLevel, string? message, Exception? exception) {
            if (logLevel < VelopackLogLevel.Information) return;
            Logger.WriteLine($"[velopack] {logLevel}: {message}{(exception != null ? Environment.NewLine + exception : "")}");
        }
    }
}

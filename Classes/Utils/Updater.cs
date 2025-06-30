using RePlays.Services;
using Squirrel;
using System;
using System.Threading.Tasks;
using static RePlays.Utils.Functions;

namespace RePlays.Utils {
    internal class Updater {
        public static string currentVersion = "?";
        public static string latestVersion = "Offline";
        public static bool applyingUpdate { get; internal set; }

        [Obsolete]
        public static async void CheckForUpdates(bool forceUpdate = false) {
            if (applyingUpdate) {
                Logger.WriteLine($"Currently in the middle of applying an update. Cannot check for updates.");
                return;
            }
            try {
                if (forceUpdate) WebInterface.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)40, (long)100);

                using var manager = await UpdateManager.GitHubUpdateManager("https://github.com/lulzsun/RePlays",
                    prerelease: SettingsService.Settings.generalSettings.updateChannel != "stable");

                if (forceUpdate) WebInterface.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)70, (long)100);

                if (manager.CurrentlyInstalledVersion() != null) {
                    currentVersion = manager.CurrentlyInstalledVersion().ToString();
                    Logger.Version = $"[v{currentVersion}]";
                }
                var updateInfo = await manager.CheckForUpdate(SettingsService.Settings.generalSettings.updateChannel != "stable"); // if nightly, we ignore deltas
                if (forceUpdate) {
                    WebInterface.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)100, (long)100);
                    await Task.Delay(500);
                    WebInterface.DestroyToast("CheckUpdateProgress");
                }
                latestVersion = updateInfo.FutureReleaseEntry.Version.ToString();
                SettingsService.SaveSettings();
                WebInterface.UpdateSettings();
                if (SettingsService.Settings.generalSettings.update == "none") return;

                if (updateInfo.ReleasesToApply.Count > 0) {
                    Action<int> progressCallback = (progressValue) => {
                        WebInterface.DisplayToast("UpdateProgress", "Installing update", "Updating", "none", (long)progressValue, (long)100);
                    };
                    if (SettingsService.Settings.generalSettings.update == "automatic") {
                        Logger.WriteLine($"New version found! Preparing to automatically update to version {updateInfo.FutureReleaseEntry.Version} from {updateInfo.CurrentlyInstalledVersion.Version}");
                        applyingUpdate = true;
                        await manager.UpdateApp(progressCallback);
                        WebInterface.DestroyToast("UpdateProgress");
                        applyingUpdate = false;
                        Logger.WriteLine($"Update to version {updateInfo.FutureReleaseEntry.Version} successful!");
                        WebInterface.DisplayModal("New update applied! Click Confirm to restart and complete the update.", "Update", "update");
                    }
                    else { // manual
                        if (forceUpdate) {
                            Logger.WriteLine($"New version found! Preparing to automatically update to version {updateInfo.FutureReleaseEntry.Version} from {updateInfo.CurrentlyInstalledVersion.Version}");
                            WebInterface.DestroyToast("ManualUpdate");
                            applyingUpdate = true;
                            await manager.UpdateApp(progressCallback);
                            WebInterface.DestroyToast("UpdateProgress");
                            applyingUpdate = false;
                            Logger.WriteLine($"Update to version {updateInfo.FutureReleaseEntry.Version} successful!");
                            WebInterface.DisplayModal("New update applied! Click Confirm to restart and complete the update.", "Update", "update");
                        }
                        else WebInterface.DisplayToast("ManualUpdate", "New version available!", "Update", "info");
                    }
                }
                else {
                    Logger.WriteLine($"Found no updates higher than current version {updateInfo.CurrentlyInstalledVersion.Version}");
                }
            }
            catch (Exception exception) {
                Logger.WriteLine("Error: Issue fetching update releases: " + exception.ToString());
                if (forceUpdate) {
                    WebInterface.DestroyToast("CheckUpdateProgress");
                    WebInterface.DisplayModal("Failed to check for update. More information written to logs.", "Error", "warning");
                }
            }
            applyingUpdate = false;
        }
    }
}
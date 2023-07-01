using RePlays.Services;
using Squirrel;
using System;

namespace RePlays.Utils {
    internal class Updater {
        public static string currentVersion = "?";
        public static string latestVersion = "Offline";
        public static bool applyingUpdate { get; internal set; }
        public static async void CheckForUpdates(bool forceUpdate = false) {
            if (applyingUpdate) {
                Logger.WriteLine($"Currently in the middle of applying an update. Cannot check for updates.");
                return;
            }
            try {
                using var manager = await UpdateManager.GitHubUpdateManager("https://github.com/lulzsun/RePlays",
                    prerelease: SettingsService.Settings.generalSettings.updateChannel != "Stable");
                if (manager.CurrentlyInstalledVersion() != null) {
                    currentVersion = manager.CurrentlyInstalledVersion().ToString();
                }
                var updateInfo = await manager.CheckForUpdate(SettingsService.Settings.generalSettings.updateChannel != "Stable"); // if nightly, we ignore deltas
                latestVersion = updateInfo.FutureReleaseEntry.Version.ToString();

                if (SettingsService.Settings.generalSettings.update == "none") return;

                if (updateInfo.ReleasesToApply.Count > 0) {
                    Action<int> progressCallback = (progressValue) => {
                        WebMessage.DisplayToast("UpdateProgress", "Installing update", "Updating", "none", (long)progressValue, (long)100);
                    };
                    if (SettingsService.Settings.generalSettings.update == "automatic") {
                        Logger.WriteLine($"New version found! Preparing to automatically update to version {updateInfo.FutureReleaseEntry.Version} from {updateInfo.CurrentlyInstalledVersion.Version}");
                        applyingUpdate = true;
                        await manager.UpdateApp(progressCallback);
                        WebMessage.DestroyToast("UpdateProgress");
                        applyingUpdate = false;
                        Logger.WriteLine($"Update to version {updateInfo.FutureReleaseEntry.Version} successful!");
                        WebMessage.DisplayModal("New update applied! Update will apply on next restart.", "Automatic Updates", "info");
                    }
                    else { // manual
                        if (forceUpdate) {
                            Logger.WriteLine($"New version found! Preparing to automatically update to version {updateInfo.FutureReleaseEntry.Version} from {updateInfo.CurrentlyInstalledVersion.Version}");
                            WebMessage.DestroyToast("ManualUpdate");
                            applyingUpdate = true;
                            await manager.UpdateApp(progressCallback);
                            WebMessage.DestroyToast("UpdateProgress");
                            applyingUpdate = false;
                            Logger.WriteLine($"Update to version {updateInfo.FutureReleaseEntry.Version} successful!");
                            WebMessage.DisplayModal("New update applied! Update will apply on next restart.", "Manual Update", "info");
                        }
                        else WebMessage.DisplayToast("ManualUpdate", "New version available!", "Update", "info");
                    }
                }
                else {
                    Logger.WriteLine($"Found no updates higher than current version {updateInfo.CurrentlyInstalledVersion.Version}");
                }
            }
            catch (System.Exception exception) {
                Logger.WriteLine("Error: Issue fetching update releases: " + exception.ToString());
            }
            applyingUpdate = false;
        }
    }
}

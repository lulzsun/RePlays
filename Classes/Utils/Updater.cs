using RePlays.Services;
using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;
using static RePlays.Utils.Functions;

namespace RePlays.Utils {
    internal class Updater {
        public static string currentVersion = "?";
        public static string latestVersion = "Offline";
        public static UpdateManager? manager;
        public static bool applyingUpdate { get; internal set; }

        [Obsolete]
        public static async void CheckForUpdates(bool forceUpdate = false) {
            if (applyingUpdate) {
                Logger.WriteLine($"Currently in the middle of applying an update. Cannot check for updates.");
                return;
            }

            bool isNightly = SettingsService.Settings.generalSettings.updateChannel != "Stable";
            try {
                if (forceUpdate) WebMessage.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)40, (long)100);

                if(manager == null)
                    manager = new UpdateManager(new GithubSource("https://github.com/lulzsun/RePlays", null, isNightly));

                if (forceUpdate) WebMessage.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)70, (long)100);

                if (manager.CurrentVersion != null) {
                    currentVersion = manager.CurrentVersion.ToString();
                }

                var updateInfo = await manager.CheckForUpdatesAsync();
                if (forceUpdate) {
                    WebMessage.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)100,
                        (long)100);
                    await Task.Delay(500);
                    WebMessage.DestroyToast("CheckUpdateProgress");
                }

                // If UpdateInfo is null, we are on the latest version
                latestVersion = updateInfo != null ? updateInfo.TargetFullRelease.Version.ToString() : currentVersion;
                SettingsService.SaveSettings();
                WebMessage.SendMessage(GetUserSettings());
                if (SettingsService.Settings.generalSettings.update == "none") return;

                if (updateInfo != null) {
                    Action<int> progressCallback = (progressValue) => {
                        WebMessage.DisplayToast("UpdateProgress", "Installing update", "Updating", "none", (long)progressValue, (long)100);
                    };
                    if (SettingsService.Settings.generalSettings.update == "automatic") {
                        Logger.WriteLine($"New version found! Preparing to automatically update to version {updateInfo.TargetFullRelease.Version} from {manager.CurrentVersion}");
                        applyingUpdate = true;
                        await manager.DownloadUpdatesAsync(updateInfo, progressCallback, isNightly);
                        WebMessage.DestroyToast("UpdateProgress");
                        applyingUpdate = false;
                        Logger.WriteLine($"Update to version {updateInfo.TargetFullRelease.Version} successful!");
                        WebMessage.DisplayModal("New update applied! Click Confirm to restart and complete the update.", "Update", "update");
                    }
                    else { // manual
                        if (forceUpdate) {
                            Logger.WriteLine($"New version found! Preparing to automatically update to version {updateInfo.TargetFullRelease.Version} from {manager.CurrentVersion}");
                            WebMessage.DestroyToast("ManualUpdate");
                            applyingUpdate = true;
                            await manager.DownloadUpdatesAsync(updateInfo, progressCallback);
                            WebMessage.DestroyToast("UpdateProgress");
                            applyingUpdate = false;
                            Logger.WriteLine($"Update to version {updateInfo.TargetFullRelease.Version} successful!");
                            WebMessage.DisplayModal("New update applied! Click Confirm to restart and complete the update.", "Update", "update");
                        }
                        else WebMessage.DisplayToast("ManualUpdate", "New version available!", "Update", "info");
                    }
                }
                else {
                    Logger.WriteLine($"Found no updates higher than current version {manager.CurrentVersion}");
                }
            }
            catch (System.Exception exception) {
                Logger.WriteLine("Error: Issue fetching update releases: " + exception.ToString());
                if (forceUpdate) {
                    WebMessage.DestroyToast("CheckUpdateProgress");
                    WebMessage.DisplayModal("Failed to check for update. More information written to logs.", "Error", "warning");
                }
            }
            applyingUpdate = false;
        }
    }
}
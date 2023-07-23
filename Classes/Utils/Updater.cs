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
        public static async void CheckForUpdates(bool forceUpdate = false) {
            if (applyingUpdate) {
                Logger.WriteLine($"Currently in the middle of applying an update. Cannot check for updates.");
                return;
            }
            try {
                if (forceUpdate) WebMessage.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)40, (long)100);

                using var manager = await UpdateManager.GitHubUpdateManager("https://github.com/lulzsun/RePlays",
                    prerelease: SettingsService.Settings.generalSettings.updateChannel != "Stable");

                if (forceUpdate) WebMessage.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)70, (long)100);

                if (manager.CurrentlyInstalledVersion() != null) {
                    currentVersion = manager.CurrentlyInstalledVersion().ToString();
                }
                var updateInfo = await manager.CheckForUpdate(SettingsService.Settings.generalSettings.updateChannel != "Stable"); // if nightly, we ignore deltas
                if (forceUpdate) {
                    WebMessage.DisplayToast("CheckUpdateProgress", "Checking for updates", "Update", "none", (long)100, (long)100);
                    await Task.Delay(500);
                    WebMessage.DestroyToast("CheckUpdateProgress");
                }
                latestVersion = updateInfo.FutureReleaseEntry.Version.ToString();
                SettingsService.SaveSettings();
                WebMessage.SendMessage(GetUserSettings());
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
                if (forceUpdate) {
                    WebMessage.DestroyToast("CheckUpdateProgress");
                    WebMessage.DisplayModal("Failed to check for update. More information written to logs.", "Error", "warning");
                }
            }
            applyingUpdate = false;
        }
    }
}

using RePlays.Services;
using Squirrel;

namespace RePlays.Utils {
    internal class Updater {
        public static string currentVersion = "?";
        public static string latestVersion = "Offline";
        public static async void CheckForUpdates() {
            try {
                var manager = await UpdateManager.GitHubUpdateManager("https://github.com/lulzsun/RePlays", null, null, null,
                    SettingsService.Settings.generalSettings.updateChannel != "stable");
                currentVersion = manager.CurrentlyInstalledVersion().ToString();
                var updateInfo = await manager.CheckForUpdate(SettingsService.Settings.generalSettings.updateChannel != "stable"); // if nightly, we ignore deltas
                latestVersion = updateInfo.FutureReleaseEntry.Version.ToString();

                if (SettingsService.Settings.generalSettings.update == "none") return;

                if (updateInfo.ReleasesToApply.Count > 0) {
                    if (SettingsService.Settings.generalSettings.update == "automatic") {
                        Logger.WriteLine($"New version found! Preparing to automatically update to version {updateInfo.FutureReleaseEntry.Version} from {updateInfo.CurrentlyInstalledVersion.Version}");
                        await manager.UpdateApp();
                        Logger.WriteLine($"Update to version {updateInfo.FutureReleaseEntry.Version} successful!");
                    }
                    else { // manual
                        WebMessage.DisplayToast("ManualUpdate", "New version available!", "Update", "info");
                    }
                }
                else {
                    Logger.WriteLine($"Found no updates higher than current version {updateInfo.CurrentlyInstalledVersion.Version}");
                }
                manager.Dispose();
            }
            catch (System.Exception exception) {
                Logger.WriteLine("Error: Issue fetching update releases: " + exception.ToString());
            }
        }
    }
}

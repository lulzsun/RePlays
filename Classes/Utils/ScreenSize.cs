#if WINDOWS
using RePlays.Services;
using System.Linq;
using System.Windows.Forms;

namespace RePlays.Utils {
    public static class ScreenSize {
        // TODO: move this somewhere else, does not need to be its own file and class
        public static void UpdateMaximumScreenResolution() {
            int screenResolution = Screen.AllScreens.Max(screen => screen.Bounds.Height);

            if (SettingsService.Settings.captureSettings.maxScreenResolution != screenResolution) {
                SettingsService.Settings.captureSettings.maxScreenResolution = screenResolution;
            }
        }
    }
}
#endif
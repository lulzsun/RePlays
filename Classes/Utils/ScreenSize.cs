#if WINDOWS
using RePlays.Services;
using System.Linq;
using System.Windows.Forms;

namespace RePlays.Utils {
    public static class ScreenSize {
        public static void UpdateMaximumScreenResolution() {
            int screenResolution = Screen.AllScreens.Max(screen => screen.Bounds.Height);

            if (SettingsService.Settings.captureSettings.maxScreenResolution != screenResolution) {
                SettingsService.Settings.captureSettings.maxScreenResolution = screenResolution;
            }
        }
    }
}
#endif
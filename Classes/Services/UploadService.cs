using RePlays.Uploaders;
using RePlays.Utils;
using System;
using static RePlays.Utils.Functions;

namespace RePlays.Services {
    public static class UploadService {
        public static async void Upload(string destination, string title, string file, string game) {
            var uploadId = GenerateShortID();
            try {
                BaseUploader uploader = (BaseUploader)Activator.CreateInstance("RePlays", $"RePlays.Uploaders.{destination}Uploader").Unwrap();
                string url = await uploader.Upload(uploadId, title, file, game);
                if (url == null) return;
                SettingsService.Settings.uploadSettings.recentLinks.Add($"[{DateTime.Now.ToShortTimeString()}] " + url);
                if (SettingsService.Settings.uploadSettings.recentLinks.Count > 10)
                    SettingsService.Settings.uploadSettings.recentLinks.RemoveAt(0);
                SettingsService.SaveSettings();
                WebMessage.SendMessage(GetUserSettings());
                Logger.WriteLine($"Successfully uploaded \"{file}\" to \"{url}\"");
            }
            catch (Exception exception) {
                Logger.WriteLine("Failed to upload clip: " + exception.ToString());
                WebMessage.DisplayModal("Failed to upload clip. More information written to logs.", "Error", "warning");
            }
            WebMessage.DestroyToast(uploadId);
        }
    }
}
using RePlays.JSONObjects;
using System;
using System.Collections.Generic;
using System.IO;
using static RePlays.Helpers.Functions;

namespace RePlays.Services {
    public static class StorageService {
        public static void ManageStorage() {
            if (!SettingsService.Settings.advancedSettings.autoManageSpace) return;
            string folderPath = SettingsService.Settings.advancedSettings.videoSaveDir;
            DriveInfo dInfo = new DriveInfo(folderPath);
            double percentOfUsedDisk = (dInfo.TotalSize - dInfo.TotalFreeSpace) / (double)dInfo.TotalSize * 100;
            double folderSizeGb = DirectorySize(new DirectoryInfo(folderPath)) / 1024f / 1024f / 1024f;

            Logger.WriteLine(string.Format("VideoSaveDir size is {0} gbs", folderSizeGb));

            if(SettingsService.Settings.advancedSettings.manageSpaceLimit == -1 || SettingsService.Settings.advancedSettings.manageTimeLimit == -1) {
                Logger.WriteLine("Automatically managing space if exceeds 90% of folder's disk");
                if(percentOfUsedDisk > 90) {
                    Logger.WriteLine("Disk is over 90% capacity, purging sessions");
                    DeleteSessionsOverSpaceLimit((int)(dInfo.TotalSize * 90 * .01 / 1024f / 1024f / 1024f), (int)((dInfo.TotalSize - dInfo.TotalFreeSpace) / 1024f / 1024f / 1024f));
                }
                return;
            }

            // if users defined settings, lets use them here
            Logger.WriteLine($"Managing space with user defined values: spaceLimit: {SettingsService.Settings.advancedSettings.manageSpaceLimit}, days: {SettingsService.Settings.advancedSettings.manageTimeLimit}");
            DeleteSessionsOverSpaceLimit(SettingsService.Settings.advancedSettings.manageSpaceLimit, folderSizeGb);
            DeleteSessionsOverMaxAge(SettingsService.Settings.advancedSettings.manageTimeLimit);
        }

        public static void DeleteSessionsOverSpaceLimit(int spaceLimitGb, double folderSizeGb) {
            if (spaceLimitGb <= 0 || folderSizeGb < spaceLimitGb) return;
            long bytesAlreadyDeleted = 0;
            Logger.WriteLine($"Sessions exceeds spaceLimit {spaceLimitGb}gbs > {folderSizeGb}gbs");

            List<Video> sessions = GetAllVideos("All Games", "Oldest", true).sessions;
            foreach (Video session in sessions) {
                if (folderSizeGb - (bytesAlreadyDeleted / 1024f / 1024f / 1024f) < spaceLimitGb) return;

                string filePath = Path.Join(SettingsService.Settings.advancedSettings.videoSaveDir, session.game, "\\", session.fileName);
                if (File.Exists(filePath)) {
                    var thumbPath = Path.Join(Path.GetDirectoryName(filePath), @"\.thumbs\", Path.GetFileNameWithoutExtension(filePath) + ".png");
                    File.Delete(filePath);
                    File.Delete(thumbPath);
                    Logger.WriteLine(filePath + " deleted due to being over spaceLimit");
                    bytesAlreadyDeleted += session.size;
                }
            }
        }

        public static void DeleteSessionsOverMaxAge(int maxAgeInDays) {
            if (maxAgeInDays <= 0) return;

            List<Video> sessions = GetAllVideos("All Games", "Oldest", true).sessions;
            foreach (Video session in sessions) {
                if (maxAgeInDays < (DateTime.Now - session.date).TotalDays) {
                    string filePath = Path.Join(SettingsService.Settings.advancedSettings.videoSaveDir, session.game, "\\", session.fileName);
                    if (File.Exists(filePath)) {
                        var thumbPath = Path.Join(Path.GetDirectoryName(filePath), @"\.thumbs\", Path.GetFileNameWithoutExtension(filePath) + ".png");
                        File.Delete(filePath);
                        File.Delete(thumbPath);
                        Logger.WriteLine(filePath + " deleted due to being over maxAge");
                    }
                } else {
                    return;
                }
            }
        }

        public static double DirectorySize(DirectoryInfo d) {
            double size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis) {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis) {
                size += DirectorySize(di);
            }
            return size;
        }
    }
}

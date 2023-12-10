using RePlays.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using static RePlays.Utils.Functions;

namespace RePlays.Services {
    public static class StorageService {
        public static void ManageStorage() {
            if (!SettingsService.Settings.storageSettings.autoManageSpace) return;
            string folderPath = GetPlaysFolder();
            DriveInfo dInfo = new DriveInfo(folderPath);
            double percentOfUsedDisk = (dInfo.TotalSize - dInfo.TotalFreeSpace) / (double)dInfo.TotalSize * 100;
            double folderSizeGb = DirectorySize(new DirectoryInfo(folderPath)) / 1024f / 1024f / 1024f;

            Logger.WriteLine(string.Format("VideoSaveDir '{0}' size is {1} gbs", folderPath, folderSizeGb));

            if (SettingsService.Settings.storageSettings.manageSpaceLimit == -1 || SettingsService.Settings.storageSettings.manageTimeLimit == -1) {
                Logger.WriteLine($"Automatically managing space if exceeds 90% of folder's disk. Currently at {percentOfUsedDisk}%");
                if (percentOfUsedDisk > 90) {
                    Logger.WriteLine("Disk is over 90% capacity, purging sessions");
                    DeleteSessionsOverSpaceLimit((int)(dInfo.TotalSize * 90 * .01 / 1024f / 1024f / 1024f), (int)((dInfo.TotalSize - dInfo.TotalFreeSpace) / 1024f / 1024f / 1024f));
                }
                return;
            }

            // if users defined settings, lets use them here
            Logger.WriteLine($"Managing space with user defined values: spaceLimit: {SettingsService.Settings.storageSettings.manageSpaceLimit}, days: {SettingsService.Settings.storageSettings.manageTimeLimit}");
            DeleteSessionsOverSpaceLimit(SettingsService.Settings.storageSettings.manageSpaceLimit, folderSizeGb);
            DeleteSessionsOverMaxAge(SettingsService.Settings.storageSettings.manageTimeLimit);
        }

        public static void DeleteSessionsOverSpaceLimit(int spaceLimitGb, double folderSizeGb) {
            if (spaceLimitGb <= 0 || folderSizeGb < spaceLimitGb) return;
            long bytesAlreadyDeleted = 0;
            Logger.WriteLine($"Sessions exceeds spaceLimit {spaceLimitGb}gbs > {folderSizeGb}gbs");

            List<Video> sessions = GetAllVideos("All Games", "Oldest", false, true).sessions;
            foreach (Video session in sessions) {
                if (folderSizeGb - (bytesAlreadyDeleted / 1024f / 1024f / 1024f) < spaceLimitGb) return;

                string filePath = Path.Join(SettingsService.Settings.storageSettings.videoSaveDir, session.game, "\\", session.fileName);
                if (File.Exists(filePath)) {
                    DeleteVideo(filePath);
                    Logger.WriteLine(filePath + " deleted due to being over spaceLimit");
                    bytesAlreadyDeleted += session.size;
                }
            }
        }

        public static void DeleteSessionsOverMaxAge(int maxAgeInDays) {
            if (maxAgeInDays <= 0) return;

            List<Video> sessions = GetAllVideos("All Games", "Oldest", false, true).sessions;
            foreach (Video session in sessions) {
                if (maxAgeInDays < (DateTime.Now - session.date).TotalDays) {
                    string filePath = Path.Join(SettingsService.Settings.storageSettings.videoSaveDir, session.game, "\\", session.fileName);
                    if (File.Exists(filePath)) {
                        DeleteVideo(filePath);
                        Logger.WriteLine(filePath + " deleted due to being over maxAge");
                    }
                }
                else {
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
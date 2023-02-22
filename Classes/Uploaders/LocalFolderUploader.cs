using RePlays.Services;
using RePlays.Utils;
using System.IO;
using System.Threading.Tasks;

namespace RePlays.Uploaders {
    public class LocalFolderUploader : BaseUploader {
        public override async Task<string> Upload(string id, string title, string file, string game) {
            var result = await Task.Run(() => {
                var destFile = Path.Combine(SettingsService.Settings.uploadSettings.localFolderSettings.dir, Path.GetFileName(file));
                byte[] buffer = new byte[1024 * 1024]; // 1MB buffer
                bool cancelFlag = false;

                using (FileStream source = new FileStream(file, FileMode.Open, FileAccess.Read)) {
                    long fileLength = source.Length;
                    using (FileStream dest = new FileStream(destFile, FileMode.CreateNew, FileAccess.Write)) {
                        long totalBytes = 0;
                        int currentBlockSize = 0;

                        while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0) {
                            totalBytes += currentBlockSize;
                            double percentage = (double)totalBytes * 100.0 / fileLength;

                            dest.Write(buffer, 0, currentBlockSize);

                            cancelFlag = false;
                            WebMessage.DisplayToast(id, title, "Upload", "none", (long)percentage, 100);

                            if (cancelFlag == true) {
                                File.Delete(destFile);
                                break;
                            }
                        }
                    }
                }
                return destFile;
            });
            return result;
        }
    }
}

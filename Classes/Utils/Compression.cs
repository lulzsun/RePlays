using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static RePlays.Utils.Functions;

namespace RePlays.Utils {
    public static class Compression {
        static Dictionary<int, double> fileTime = new Dictionary<int, double>();
        public static void CompressFile(string filePath, string game) {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = Path.Join(GetFFmpegFolder(), "ffmpeg.exe"),
                Arguments = string.Format("-i \"{0}\" -vcodec libx264 -crf 28 \"{1}\"", filePath, filePath.Replace(".mkv", "-compressed.mkv").Replace(".mp4", "-compressed.mp4")),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            Process process = new Process {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (s, e) => ffmpeg_input(e.Data, process, game);
            process.ErrorDataReceived += (s, e) => ffmpeg_input(e.Data, process, game);
            process.Start();
            process.Exited += async (sender, e) => await p_ExitedAsync(sender, e, filePath, process);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            fileTime.Add(process.Id, 0);

            WebMessage.DisplayToast(process.Id.ToString(), game, "Compressing", "none", (long)0, 100);
        }

        static void ffmpeg_input(string e, Process process, string game) {
            if (e == null)
                return;

            if (e.Contains("Duration: ")) {
                fileTime[process.Id] = TimeSpan.Parse(e.ToString().Trim().Substring(10, 11)).TotalSeconds;
            }

            if (e.Contains("frame=") && e.Contains("speed=") && !e.Contains("Lsize=")) {
                Logger.WriteLine(e);
                try {
                    WebMessage.DisplayToast(process.Id.ToString(), game, "Compressing", "none", Convert.ToInt32(TimeSpan.Parse(e.Trim().Substring(48, 11)).TotalSeconds), Convert.ToInt32(fileTime[process.Id]));
                }
                catch (Exception ex) {
                    Logger.WriteLine("Error: {}", ex.Message);
                }
            }
        }

        static async Task p_ExitedAsync(object sender, EventArgs e, string filePathOriginal, Process process) {
            WebMessage.DestroyToast(process.Id.ToString());
            process.Kill();

            string filePathCompressed = filePathOriginal.Replace(".mp4", "-compressed.mp4");

            long originalFileSize = new FileInfo(filePathOriginal).Length;
            long compressedFileSize = new FileInfo(filePathCompressed).Length;

            if (compressedFileSize > originalFileSize || compressedFileSize == 0) {
                if (compressedFileSize == 0) WebMessage.DisplayModal("Failed to compress the file", "Error", "warning");
                if (compressedFileSize > originalFileSize) WebMessage.DisplayModal("The compressed file turned out to be larger than the original file. We will keep the original file.", "Compression size", "warning");
                System.IO.File.Delete(filePathCompressed);
                return;
            }

            try {
                System.IO.File.Delete(filePathOriginal);
                System.IO.File.Move(filePathCompressed, filePathOriginal);
            }
            catch (Exception ex) {
                Logger.WriteLine($"Error: {ex.Message}");
                WebMessage.DisplayModal("Failed to compress the file", "Error", "warning");
                return;
            }

            var t = await Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy));
            Logger.WriteLine(t);
            WebMessage.SendMessage(t);

            WebMessage.DisplayModal("Successfully compressed the file from " + GetReadableFileSize(originalFileSize) + " to " + GetReadableFileSize(compressedFileSize), "Success", "success");

        }
    }
}
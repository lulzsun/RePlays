using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static RePlays.Utils.Functions;

namespace RePlays.Utils {
    public static class Compression {
        static Dictionary<int, double> fileTime = new Dictionary<int, double>();
        public static void CompressFile(string filePath, CompressClip data) {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = Path.Join(GetFFmpegFolder(), "ffmpeg"),
                Arguments = string.Format("-i \"{0}\" -vcodec libx264 -preset \"{1}\" \"{2}\"", filePath, data.quality, filePath.Replace(".mkv", "-compressed.mkv").Replace(".mp4", "-compressed.mp4")),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            Process process = new Process {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (s, e) => ffmpeg_input(e.Data, process, data.game);
            process.ErrorDataReceived += (s, e) => ffmpeg_input(e.Data, process, data.game);
            process.Start();
            process.Exited += async (sender, e) => await p_ExitedAsync(sender, e, filePath, process);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            fileTime.Add(process.Id, 0);

            WebMessage.DisplayToast(process.Id.ToString(), data.game, "Compressing", "none", (long)0, 100);
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

            var startInfo = new ProcessStartInfo {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                FileName = Path.Join(GetFFmpegFolder(), "ffprobe"),
                Arguments = $"-v error -i \"{compressedFileSize}\""
            };

            using var verifyProcess = Process.Start(startInfo);
            string output = verifyProcess.StandardOutput.ReadToEnd();
            Logger.WriteLine("Output: " + output);
            verifyProcess.WaitForExit();

            if (compressedFileSize > originalFileSize || compressedFileSize == 0 || !string.IsNullOrWhiteSpace(output)) {
                if (compressedFileSize == 0 || !string.IsNullOrWhiteSpace(output)) WebMessage.DisplayModal("Failed to compress the file", "Error", "warning");
                if (compressedFileSize > originalFileSize) WebMessage.DisplayModal("The compressed file turned out to be larger than the original file. We will keep the original file.", "Compression size", "warning");
                File.Delete(filePathCompressed);
                return;
            }

            try {
                File.Delete(filePathOriginal);
                File.Move(filePathCompressed, filePathOriginal);
            }
            catch (Exception ex) {
                Logger.WriteLine($"Error: {ex.Message}");
                WebMessage.DisplayModal("Failed to compress the file", "Error", "warning");
                return;
            }

#if RELEASE && WINDOWS
            var t = await Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy, true));
#else
            var t = await Task.Run(() => GetAllVideos(WebMessage.videoSortSettings.game, WebMessage.videoSortSettings.sortBy));
#endif

            Logger.WriteLine(t);
            WebMessage.SendMessage(t);

            WebMessage.DisplayModal("Successfully compressed the file from " + GetReadableFileSize(originalFileSize) + " to " + GetReadableFileSize(compressedFileSize), "Success", "success");

        }
    }
}
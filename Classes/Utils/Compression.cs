using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static RePlays.Utils.Functions;

namespace RePlays.Utils
{
    public static class Compression
    {
        static Dictionary<int, double> fileTime = new Dictionary<int, double>();
        public static void CompressFile(string filePath, string game)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Join(GetFFmpegFolder(), "ffmpeg.exe"),
                Arguments = string.Format("-i \"{0}\" -vcodec libx265 -crf 28 \"{1}\"", filePath, filePath.Replace(".mp4", "-compressed.mp4")),            
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            Process process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (s, e) => ffmpeg_input(e.Data, process, game);
            process.ErrorDataReceived += (s, e) => ffmpeg_input(e.Data, process, game);
            process.Start();
            process.Exited += (sender, e) => p_Exited(sender, e, filePath, process);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            fileTime.Add(process.Id, 0);

            WebMessage.DisplayToast(process.Id.ToString(), game, "Compressing", "none", (long)0, 100);
        }

        static void ffmpeg_input(string e, Process process, string game)
        {
            if (e == null)
                return;

            if (e.Contains("Duration: "))
            {
                fileTime[process.Id] = TimeSpan.Parse(e.ToString().Trim().Substring(10, 11)).TotalSeconds;
            }

            if (e.Contains("frame=") && e.Contains("speed=") && !e.Contains("Lsize="))
            {              
                Logger.WriteLine(e);
                try
                {
                    WebMessage.DisplayToast(process.Id.ToString(), game, "Compressing", "none", Convert.ToInt32(TimeSpan.Parse(e.Trim().Substring(48, 11)).TotalSeconds), Convert.ToInt32(fileTime[process.Id]));
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("Error: {}", ex.Message);
                }
            }
        }

        static void p_Exited(object sender, EventArgs e, string filePath, Process process)
        {
            WebMessage.DestroyToast(process.Id.ToString());
            Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath.Replace(".mp4", "-compressed.mp4")));
            process.Dispose();
        }
    }
}

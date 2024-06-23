using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RePlays.Utils {
    public static class Logger {
        public static bool IsConsole = false;
        private static Object thisLock = new Object();

        public static string Version = "";

        public static void WriteLine(string message,
                [CallerFilePath] string file = null,
                [CallerMemberName] string memberName = "",
                [CallerLineNumber] int line = 0) {
            string logLine = $"[{DateTime.Now}]{Version}[{Path.GetFileName(file)}::{memberName}({line})]: {message}";
            if (IsConsole) {
                Console.WriteLine(logLine);
                System.Diagnostics.Debug.WriteLine(logLine);
            }
            else {
                lock (thisLock) {
                    string logFile = Path.Join(Functions.GetCfgFolder(), "/logs.txt");
                    File.AppendAllText(logFile, logLine + Environment.NewLine);
                }
            }
        }

        public static void Purge() {
            try {
                string logFile = Path.Join(Functions.GetCfgFolder(), "/logs.txt");
                var logFileContents = File.ReadAllLines(logFile);

                if (logFileContents.Length > 2000) {
                    var newLogs = File.ReadAllLines(logFile).Skip(logFileContents.Length / 2).ToList();
                    newLogs.Insert(0, "--- Purged Logs ---");
                    File.WriteAllLines(logFile, newLogs.ToArray());
                }
            }
            catch (Exception e) {
                WriteLine($"Failed to purge logs file, reason: {e.Message}");
            }
        }
    }
}
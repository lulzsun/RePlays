using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace RePlays.Services {
    public static class Logger {
        public static bool IsConsole = false;
        public static void WriteLine(string message,
                [CallerFilePath] string file = null,
                [CallerLineNumber] int line = 0) {
            string logLine = string.Format("[{0}][{1}({2})]: {3}", DateTime.UtcNow, Path.GetFileName(file), line, message);
            if(IsConsole)
                Console.WriteLine(logLine);
            else
                File.AppendAllText(Application.StartupPath + @"\logs.txt", logLine + Environment.NewLine);
        }
    }
}
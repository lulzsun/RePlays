using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace RePlays.Services {
    public static class Logger {
        public static void WriteLine(string message,
                [CallerFilePath] string file = null,
                [CallerLineNumber] int line = 0) {
            Console.WriteLine("[{0}][{1}({2})]: {3}", DateTime.UtcNow, Path.GetFileName(file), line, message);
        }
    }
}
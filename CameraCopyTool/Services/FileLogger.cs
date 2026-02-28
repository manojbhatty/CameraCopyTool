using System;
using System.IO;

namespace CameraCopyTool.Services
{
    /// <summary>
    /// Simple file-based logger for debugging.
    /// </summary>
    public static class FileLogger
    {
        private static readonly string LogFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "logs",
            $"upload-{DateTime.UtcNow:yyyy-MM-dd}.log");
        
        // Maximum log file size: read from user settings (default: 5 MB)
        private static readonly long MaxLogFileSize = Properties.Settings.Default.DebugLogMaxFileSize;

        static FileLogger()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        /// <summary>
        /// Writes a log message to the log file.
        /// </summary>
        public static void Log(string message)
        {
            try
            {
                // Check if log file exists and exceeds max size
                if (File.Exists(LogFilePath))
                {
                    var fileInfo = new FileInfo(LogFilePath);
                    if (fileInfo.Length >= MaxLogFileSize)
                    {
                        // Skip logging - file is too large
                        return;
                    }
                }
                
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, logEntry);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        /// <summary>
        /// Gets the path to the current log file.
        /// </summary>
        public static string GetLogFilePath() => LogFilePath;
    }
}

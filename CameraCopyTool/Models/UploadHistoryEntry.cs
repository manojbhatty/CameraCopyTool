using System;
using System.Security.Cryptography;
using System.IO;

namespace CameraCopyTool.Models
{
    /// <summary>
    /// Status of an upload history entry.
    /// </summary>
    public enum UploadHistoryStatus
    {
        Success,
        Failed,
        Cancelled,
        Skipped,
        LocalFileDeleted,
        FileChanged
    }

    /// <summary>
    /// Represents an entry in the upload history log.
    /// </summary>
    public class UploadHistoryEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier for this entry.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the timestamp when the upload was attempted.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full path to the file.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the file in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the SHA256 hash of the file at upload time.
        /// </summary>
        public string? FileHash { get; set; }

        /// <summary>
        /// Gets or sets the Google Drive file ID if uploaded successfully.
        /// </summary>
        public string? GoogleDriveFileId { get; set; }

        /// <summary>
        /// Gets or sets the upload status.
        /// </summary>
        public UploadHistoryStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the error message if the upload failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the error category if the upload failed.
        /// </summary>
        public string? ErrorCategory { get; set; }

        /// <summary>
        /// Gets or sets the duration of the upload in seconds.
        /// </summary>
        public double DurationSeconds { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the file was last verified to exist.
        /// </summary>
        public DateTime? LastVerified { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the entry was marked for cleanup (e.g., file deleted).
        /// </summary>
        public DateTime? MarkedForCleanup { get; set; }

        /// <summary>
        /// Gets a human-readable file size string.
        /// </summary>
        public string FileSizeString => FormatFileSize(FileSize);

        /// <summary>
        /// Gets a human-readable timestamp string.
        /// </summary>
        public string TimestampString => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// Gets a human-readable duration string.
        /// </summary>
        public string DurationString
        {
            get
            {
                if (DurationSeconds < 60)
                    return $"{DurationSeconds:F1}s";
                if (DurationSeconds < 3600)
                {
                    var minutes = (int)(DurationSeconds / 60);
                    var seconds = (int)(DurationSeconds % 60);
                    return $"{minutes}m {seconds}s";
                }
                var hours = (int)(DurationSeconds / 3600);
                var mins = (int)((DurationSeconds % 3600) / 60);
                return $"{hours}h {mins}m";
            }
        }

        /// <summary>
        /// Gets a human-readable upload date string for tooltips.
        /// </summary>
        public string UploadedDateString => $"Uploaded to Google Drive on {Timestamp:MMM dd, yyyy h:mm tt}";

        /// <summary>
        /// Computes the SHA256 hash of a file.
        /// </summary>
        public static string? ComputeFileHash(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if the local file has changed since upload by comparing hashes.
        /// </summary>
        public bool HasFileChanged()
        {
            if (string.IsNullOrEmpty(FileHash) || !File.Exists(FilePath))
                return false;

            var currentHash = ComputeFileHash(FilePath);
            return currentHash != FileHash;
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}

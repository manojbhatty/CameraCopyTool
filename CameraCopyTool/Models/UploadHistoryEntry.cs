using System;

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
        Skipped
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

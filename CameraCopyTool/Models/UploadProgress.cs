namespace CameraCopyTool.Models
{
    /// <summary>
    /// Represents progress information for file uploads.
    /// </summary>
    public class UploadProgress
    {
        /// <summary>
        /// Gets or sets the upload percentage (0-100).
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes sent so far.
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// Gets or sets the total file size in bytes.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets the upload speed in bytes per second.
        /// </summary>
        public double BytesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the estimated time remaining in seconds.
        /// </summary>
        public double SecondsRemaining { get; set; }

        /// <summary>
        /// Gets a human-readable upload speed string (e.g., "2.5 MB/s").
        /// </summary>
        public string SpeedString => FormatBytesPerSecond(BytesPerSecond);

        /// <summary>
        /// Gets a human-readable time remaining string (e.g., "2m 30s").
        /// </summary>
        public string TimeRemainingString => FormatTimeRemaining(SecondsRemaining);

        /// <summary>
        /// Formats bytes per second to a human-readable string.
        /// </summary>
        private static string FormatBytesPerSecond(double bytesPerSecond)
        {
            if (bytesPerSecond < 0) return "Calculating...";
            if (bytesPerSecond < 1024) return $"{bytesPerSecond:F0} B/s";
            if (bytesPerSecond < 1024 * 1024) return $"{bytesPerSecond / 1024:F1} KB/s";
            if (bytesPerSecond < 1024 * 1024 * 1024) return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
            return $"{bytesPerSecond / (1024 * 1024 * 1024):F1} GB/s";
        }

        /// <summary>
        /// Formats seconds remaining to a human-readable string.
        /// </summary>
        private static string FormatTimeRemaining(double seconds)
        {
            if (seconds < 0) return "Calculating...";
            if (seconds < 60) return $"{(int)seconds}s";
            if (seconds < 3600)
            {
                var minutes = (int)(seconds / 60);
                var secs = (int)(seconds % 60);
                return $"{minutes}m {secs}s";
            }
            
            var hours = (int)(seconds / 3600);
            var mins = (int)((seconds % 3600) / 60);
            return $"{hours}h {mins}m";
        }
    }

    /// <summary>
    /// Represents the result of a file upload operation.
    /// </summary>
    public class UploadResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the upload was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the Google Drive file ID of the uploaded file.
        /// </summary>
        public string? FileId { get; set; }

        /// <summary>
        /// Gets or sets the name of the uploaded file.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Gets or sets the size of the uploaded file in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets an error message if the upload failed.
        /// </summary>
        public string? Error { get; set; }
    }
}

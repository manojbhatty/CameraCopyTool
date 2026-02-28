using System;

namespace CameraCopyTool.Models
{
    /// <summary>
    /// Categories of upload errors for handling and recovery.
    /// </summary>
    public enum ErrorCategory
    {
        /// <summary>
        /// Unknown or unclassified error.
        /// </summary>
        Unknown,

        /// <summary>
        /// Network-related errors (connection lost, timeout).
        /// </summary>
        Network,

        /// <summary>
        /// Google Drive API errors (quota exceeded, rate limit).
        /// </summary>
        Api,

        /// <summary>
        /// File system errors (file in use, not found, disk space).
        /// </summary>
        File,

        /// <summary>
        /// Authentication errors (token expired, revoked).
        /// </summary>
        Authentication,

        /// <summary>
        /// User-initiated cancellation.
        /// </summary>
        UserCancellation
    }

    /// <summary>
    /// Represents an upload error with detailed information.
    /// </summary>
    public class UploadError
    {
        /// <summary>
        /// Gets or sets the error category.
        /// </summary>
        public ErrorCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the detailed error description.
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code if applicable.
        /// </summary>
        public int? HttpStatusCode { get; set; }

        /// <summary>
        /// Gets or sets the inner exception.
        /// </summary>
        public Exception? InnerException { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the error is retryable.
        /// </summary>
        public bool IsRetryable { get; set; }

        /// <summary>
        /// Gets or sets the suggested retry delay in milliseconds.
        /// </summary>
        public int? RetryDelayMs { get; set; }

        /// <summary>
        /// Creates an UploadError from an exception.
        /// </summary>
        public static UploadError FromException(Exception ex, int? httpStatusCode = null)
        {
            var error = new UploadError
            {
                Message = ex.Message,
                Details = ex.ToString(),
                InnerException = ex,
                HttpStatusCode = httpStatusCode
            };

            // Categorize the error
            error.Category = CategorizeException(ex, httpStatusCode);

            // Set retryability and delay based on category
            switch (error.Category)
            {
                case ErrorCategory.Network:
                    error.IsRetryable = true;
                    error.RetryDelayMs = 1000; // Start with 1s, will use exponential backoff
                    break;
                case ErrorCategory.Api:
                    // Rate limits and quota errors are retryable
                    error.IsRetryable = httpStatusCode == 429 || httpStatusCode == 503;
                    error.RetryDelayMs = httpStatusCode == 429 ? 5000 : 2000;
                    break;
                case ErrorCategory.Authentication:
                    error.IsRetryable = false; // Requires re-authentication
                    break;
                case ErrorCategory.File:
                    error.IsRetryable = true; // May be transient (e.g., file locked)
                    error.RetryDelayMs = 500;
                    break;
                case ErrorCategory.UserCancellation:
                    error.IsRetryable = false;
                    break;
                default:
                    error.IsRetryable = false;
                    break;
            }

            return error;
        }

        /// <summary>
        /// Categorizes an exception into an ErrorCategory.
        /// </summary>
        private static ErrorCategory CategorizeException(Exception ex, int? httpStatusCode)
        {
            var exType = ex.GetType().Name;

            // Check for TaskCanceledException first (user cancellation)
            if (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                return ErrorCategory.UserCancellation;
            }

            // Network-related exceptions
            if (ex is System.Net.WebException ||
                ex is System.IO.IOException && ex.Message.Contains("network") ||
                exType.Contains("WebException") ||
                exType.Contains("HttpRequestException"))
            {
                return ErrorCategory.Network;
            }

            // Google API exceptions
            if (exType.Contains("GoogleApi") || httpStatusCode.HasValue)
            {
                if (httpStatusCode == 401 || httpStatusCode == 403)
                {
                    return ErrorCategory.Authentication;
                }

                if (httpStatusCode == 429 || httpStatusCode == 503)
                {
                    return ErrorCategory.Api; // Rate limit / quota
                }

                if (httpStatusCode >= 500)
                {
                    return ErrorCategory.Network; // Server errors
                }

                return ErrorCategory.Api;
            }

            // File system exceptions
            if (ex is System.IO.IOException ||
                ex is System.UnauthorizedAccessException ||
                exType.Contains("IOException") ||
                exType.Contains("FileNotFoundException") ||
                exType.Contains("DirectoryNotFoundException"))
            {
                return ErrorCategory.File;
            }

            return ErrorCategory.Unknown;
        }

        /// <summary>
        /// Gets a user-friendly title for the error.
        /// </summary>
        public string GetTitle()
        {
            return Category switch
            {
                ErrorCategory.Network => "Network Error",
                ErrorCategory.Api => "Upload Limit Reached",
                ErrorCategory.File => "File Error",
                ErrorCategory.Authentication => "Authentication Required",
                ErrorCategory.UserCancellation => "Upload Cancelled",
                _ => "Upload Error"
            };
        }

        /// <summary>
        /// Gets a user-friendly message for the error.
        /// </summary>
        public string GetUserMessage()
        {
            return Category switch
            {
                ErrorCategory.Network => "No internet connection. The upload will pause and resume automatically when connection is restored.",
                ErrorCategory.Api when HttpStatusCode == 429 => "You've reached your upload limit for Google Drive. Please wait a few minutes and try again.",
                ErrorCategory.Api when HttpStatusCode == 503 => "Google Drive is temporarily unavailable. Retrying...",
                ErrorCategory.File when Message.Contains("disk space") => "Not enough free disk space to prepare this file for upload.",
                ErrorCategory.File => $"Cannot access the file: {Message}",
                ErrorCategory.Authentication => "Your session has expired. Please sign in again.",
                ErrorCategory.UserCancellation => "Upload was cancelled by the user.",
                _ => $"Upload failed: {Message}"
            };
        }
    }
}

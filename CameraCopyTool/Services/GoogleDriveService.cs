using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Requests;
using CameraCopyTool.Models;

namespace CameraCopyTool.Services
{
    /// <summary>
    /// Event args for upload error events.
    /// </summary>
    public class UploadErrorEventArgs : EventArgs
    {
        public Models.UploadError Error { get; set; } = null!;
        public bool ShouldRetry { get; set; }
        public int RetryDelayMs { get; set; }
    }

    /// <summary>
    /// Service for handling Google Drive authentication and API operations.
    /// </summary>
    public interface IGoogleDriveService
    {
        /// <summary>
        /// Gets a value indicating whether the user is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the authenticated user's email address.
        /// </summary>
        string? UserEmail { get; }

        /// <summary>
        /// Event raised when an upload error occurs.
        /// </summary>
        event EventHandler<UploadErrorEventArgs>? UploadError;

        /// <summary>
        /// Authenticates the user with Google Drive using OAuth 2.0.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if authentication succeeded, false otherwise.</returns>
        Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs out the user by deleting stored credentials.
        /// </summary>
        void Logout();

        /// <summary>
        /// Uploads a file to Google Drive with retry support.
        /// </summary>
        /// <param name="filePath">Path to the file to upload.</param>
        /// <param name="progress">Progress reporter for upload progress (percentage, speed, time remaining).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Upload result with file ID and status, or null if failed.</returns>
        Task<UploadResult?> UploadFileAsync(
            string filePath,
            IProgress<UploadProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a file to Google Drive with explicit retry control.
        /// </summary>
        /// <param name="filePath">Path to the file to upload.</param>
        /// <param name="onError">Callback for error handling. Return true to retry, false to fail. Receives error, retry count, and max retries.</param>
        /// <param name="progress">Progress reporter for upload progress (percentage, speed, time remaining).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Upload result with file ID and status, or null if failed.</returns>
        Task<UploadResult?> UploadFileWithRetryAsync(
            string filePath,
            Func<UploadError, int, int, Task<bool>> onError,
            IProgress<UploadProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of IGoogleDriveService using Google Drive API v3.
    /// </summary>
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly GoogleDriveSettings _settings;
        private readonly IUploadHistoryService _uploadHistoryService;
        private readonly INetworkService _networkService;
        private UserCredential? _credential;
        private DriveService? _driveService;
        private readonly int _maxRetries;
        private readonly int[] _retryDelaysMs;

        public GoogleDriveService(GoogleDriveSettings settings, IUploadHistoryService uploadHistoryService, INetworkService networkService, int maxRetries = 5)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _uploadHistoryService = uploadHistoryService ?? throw new ArgumentNullException(nameof(uploadHistoryService));
            _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
            _maxRetries = maxRetries;
            // Exponential backoff: 1s, 2s, 4s, 8s, 16s
            _retryDelaysMs = new int[maxRetries];
            for (int i = 0; i < maxRetries; i++)
            {
                _retryDelaysMs[i] = (int)Math.Min(1000 * Math.Pow(2, i), 30000); // Cap at 30s
            }
        }

        public bool IsAuthenticated => _credential != null;

        public string? UserEmail
        {
            get
            {
                // Try to get email from user info
                var email = _credential?.UserId;

                // If it's just "user" or empty, return generic message
                if (string.IsNullOrEmpty(email) || email == "user")
                {
                    return "Google Account";
                }

                return email;
            }
        }

        public event EventHandler<UploadErrorEventArgs>? UploadError;

        /// <summary>
        /// Authenticates the user with Google Drive using OAuth 2.0.
        /// </summary>
        public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate configuration
                if (!_settings.IsConfigured)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Google Drive credentials not configured. Please set ClientId and ClientSecret in App.config.");
                    return false;
                }

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(_settings.CredentialsPath)!);

                // Create client secrets from config
                var clientSecrets = new ClientSecrets
                {
                    ClientId = _settings.ClientId!,
                    ClientSecret = _settings.ClientSecret!
                };

                // Authenticate using OAuth 2.0
                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    new[] { _settings.Scope },
                    "user",
                    cancellationToken,
                    new FileDataStore(_settings.CredentialsPath, true));

                // Create Drive service
                _driveService = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = _settings.ApplicationName
                });

                return true;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Logs out the user by deleting stored credentials.
        /// </summary>
        public void Logout()
        {
            try
            {
                // FileDataStore stores tokens in subdirectories under CredentialsPath
                // Delete the entire directory to clear all cached tokens
                if (Directory.Exists(_settings.CredentialsPath))
                {
                    Directory.Delete(_settings.CredentialsPath, true);
                }

                _credential = null;
                _driveService = null;
                
                System.Diagnostics.Debug.WriteLine($"Logout successful - deleted: {_settings.CredentialsPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Uploads a file to Google Drive.
        /// </summary>
        public async Task<UploadResult?> UploadFileAsync(
            string filePath,
            IProgress<UploadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return await UploadFileWithRetryAsync(filePath, (_, _, _) => Task.FromResult(false), progress, cancellationToken);
        }

        /// <summary>
        /// Uploads a file to Google Drive with retry support.
        /// </summary>
        public async Task<UploadResult?> UploadFileWithRetryAsync(
            string filePath,
            Func<UploadError, int, int, Task<bool>> onError,
            IProgress<UploadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated || _driveService == null)
            {
                throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
            }

            var startTime = DateTime.Now;
            int retryCount = 0;
            UploadError? lastError = null;
            UploadResult? finalResult = null;

            while (retryCount <= _maxRetries)
            {
                try
                {
                    var result = await UploadFileInternalAsync(filePath, progress, cancellationToken);

                    if (result.Success)
                    {
                        finalResult = result;
                        break;
                    }

                    // Upload failed - create error from result
                    lastError = new UploadError
                    {
                        Category = ErrorCategory.Unknown,
                        Message = result.Error ?? "Unknown error",
                        IsRetryable = retryCount < _maxRetries
                    };

                    // Categorize the error based on the error message
                    if (result.Error != null)
                    {
                        if (result.Error.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                            result.Error.Contains("connection", StringComparison.OrdinalIgnoreCase))
                        {
                            lastError.Category = ErrorCategory.Network;
                            lastError.IsRetryable = true;
                        }
                        else if (result.Error.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                                 result.Error.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                        {
                            lastError.Category = ErrorCategory.Api;
                            lastError.HttpStatusCode = 429;
                            lastError.IsRetryable = true;
                        }
                        else if (result.Error.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                                 result.Error.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                        {
                            lastError.Category = ErrorCategory.Authentication;
                            lastError.IsRetryable = false;
                        }
                        else if (result.Error.Contains("file", StringComparison.OrdinalIgnoreCase) ||
                                 result.Error.Contains("disk", StringComparison.OrdinalIgnoreCase))
                        {
                            lastError.Category = ErrorCategory.File;
                            lastError.IsRetryable = true;
                        }
                    }
                }
                catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    lastError = new UploadError
                    {
                        Category = ErrorCategory.UserCancellation,
                        Message = "Upload was cancelled by the user",
                        IsRetryable = false
                    };
                    finalResult = new UploadResult
                    {
                        Success = false,
                        Error = "Upload was cancelled by the user"
                    };
                    break;
                }
                catch (Exception ex)
                {
                    lastError = Models.UploadError.FromException(ex);
                }

                // Log the error
                FileLogger.Log($"Upload attempt {retryCount + 1} failed: {lastError.Message}");
                System.Diagnostics.Debug.WriteLine($"Upload attempt {retryCount + 1} failed: {lastError.Message}");

                // Check if we should retry
                if (retryCount >= _maxRetries || !lastError.IsRetryable)
                {
                    // Max retries reached or non-retryable error
                    return new UploadResult
                    {
                        Success = false,
                        Error = lastError.GetUserMessage()
                    };
                }

                // Notify error handler
                var errorArgs = new UploadErrorEventArgs
                {
                    Error = lastError,
                    ShouldRetry = true,
                    RetryDelayMs = _retryDelaysMs[retryCount]
                };

                // Call external error handler if provided
                if (onError != null)
                {
                    errorArgs.ShouldRetry = await onError(lastError, retryCount + 1, _maxRetries);
                }

                // Raise event
                UploadError?.Invoke(this, errorArgs);

                if (!errorArgs.ShouldRetry)
                {
                    return new UploadResult
                    {
                        Success = false,
                        Error = lastError.GetUserMessage()
                    };
                }

                // For network errors, wait for network to be restored before retrying
                if (lastError.Category == ErrorCategory.Network)
                {
                    FileLogger.Log("Network error - waiting for connection to restore...");
                    System.Diagnostics.Debug.WriteLine("Network error - waiting for connection to restore...");
                    
                    try
                    {
                        await _networkService.WaitForNetworkAsync(cancellationToken);
                        FileLogger.Log("Network restored - resuming upload");
                        System.Diagnostics.Debug.WriteLine("Network restored - resuming upload");
                    }
                    catch (OperationCanceledException)
                    {
                        FileLogger.Log("Network wait cancelled");
                        return new UploadResult
                        {
                            Success = false,
                            Error = "Upload cancelled while waiting for network"
                        };
                    }
                }
                else
                {
                    // For other errors, wait before retrying (exponential backoff)
                    var delay = errorArgs.RetryDelayMs > 0 ? errorArgs.RetryDelayMs : _retryDelaysMs[retryCount];
                    FileLogger.Log($"Retrying in {delay}ms...");
                    System.Diagnostics.Debug.WriteLine($"Retrying in {delay}ms...");
                    await Task.Delay(delay, cancellationToken);
                }
                
                retryCount++;
            }

            // Log to upload history
            await LogUploadToHistory(filePath, finalResult, lastError, startTime);

            return finalResult ?? new UploadResult
            {
                Success = false,
                Error = lastError?.GetUserMessage() ?? "Upload failed after maximum retries"
            };
        }

        /// <summary>
        /// Logs the upload result to the upload history.
        /// </summary>
        private async Task LogUploadToHistory(string filePath, UploadResult? result, UploadError? error, DateTime startTime)
        {
            try
            {
                FileLogger.Log($"LogUploadToHistory called: filePath='{filePath}', result.Success={result?.Success}, error={error?.Category}");
                
                // Don't log if file path is empty or null
                if (string.IsNullOrEmpty(filePath))
                {
                    FileLogger.Log("Skipping history log: file path is empty");
                    return;
                }
                
                if (!File.Exists(filePath))
                {
                    FileLogger.Log($"Skipping history log: file not found - {filePath}");
                    return;
                }
                
                // Don't log if upload was never attempted (no result and no error)
                if (result == null && error == null)
                {
                    FileLogger.Log("Skipping history log: no result and no error (upload not attempted)");
                    return;
                }
                
                var fileName = Path.GetFileName(filePath);
                var fileSize = new FileInfo(filePath).Length;
                var duration = (DateTime.Now - startTime).TotalSeconds;

                FileLogger.Log($"Creating history entry: fileName={fileName}, fileSize={fileSize}, duration={duration}");

                var historyEntry = new UploadHistoryEntry
                {
                    FileName = fileName,
                    FilePath = filePath,
                    FileSize = fileSize,
                    FileHash = UploadHistoryEntry.ComputeFileHash(filePath),
                    GoogleDriveFileId = result?.FileId,
                    Status = result?.Success == true ? UploadHistoryStatus.Success :
                             error?.Category == ErrorCategory.UserCancellation ? UploadHistoryStatus.Cancelled :
                             UploadHistoryStatus.Failed,
                    ErrorMessage = result?.Error ?? error?.Message,
                    ErrorCategory = error?.Category.ToString(),
                    DurationSeconds = duration,
                    LastVerified = DateTime.Now
                };

                await _uploadHistoryService.AddEntryAsync(historyEntry);
                FileLogger.Log("History entry saved successfully");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Failed to log upload to history: {ex.Message}");
            }
        }

        /// <summary>
        /// Internal method to perform a single upload attempt.
        /// </summary>
        private async Task<UploadResult> UploadFileInternalAsync(
            string filePath,
            IProgress<UploadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var fileName = Path.GetFileName(filePath);
            var fileSize = new FileInfo(filePath).Length;

            FileLogger.Log($"Starting upload: {fileName} ({fileSize} bytes)");
            System.Diagnostics.Debug.WriteLine($"Starting upload: {fileName} ({fileSize} bytes)");

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName
            };

            var uploadRequest = _driveService.Files.Create(fileMetadata, fileStream, "application/octet-stream");
            uploadRequest.Fields = "id";

            // Track progress
            uploadRequest.ProgressChanged += (uploadProgress) =>
            {
                var logMsg = $"Progress: {uploadProgress.Status} - {uploadProgress.BytesSent} bytes";
                FileLogger.Log(logMsg);
                System.Diagnostics.Debug.WriteLine(logMsg);

                progress?.Report(new UploadProgress
                {
                    Percentage = fileSize > 0 ? Math.Round((double)uploadProgress.BytesSent / fileSize * 100, 1) : 0,
                    BytesSent = uploadProgress.BytesSent,
                    TotalBytes = fileSize,
                    BytesPerSecond = 0,
                    SecondsRemaining = 0
                });
            };

            FileLogger.Log("Upload request sent...");
            System.Diagnostics.Debug.WriteLine("Upload request sent...");

            Google.Apis.Drive.v3.Data.File? uploadedFile = null;

            try
            {
                var uploadResult = await uploadRequest.UploadAsync(cancellationToken);

                FileLogger.Log($"Upload completed: {uploadResult.Status}");
                System.Diagnostics.Debug.WriteLine($"Upload completed: {uploadResult.Status}");

                if (uploadResult.Status == Google.Apis.Upload.UploadStatus.Completed)
                {
                    uploadedFile = uploadRequest.ResponseBody;
                    FileLogger.Log($"File uploaded successfully: {uploadedFile?.Id}");
                    System.Diagnostics.Debug.WriteLine($"File uploaded successfully: {uploadedFile?.Id}");

                    // Final progress report
                    progress?.Report(new UploadProgress
                    {
                        Percentage = 100.0,
                        BytesSent = fileSize,
                        TotalBytes = fileSize,
                        BytesPerSecond = 0,
                        SecondsRemaining = 0
                    });

                    return new UploadResult
                    {
                        Success = true,
                        FileId = uploadedFile?.Id,
                        FileName = fileName,
                        FileSize = fileSize
                    };
                }

                if (uploadResult.Exception != null)
                {
                    FileLogger.Log($"Upload exception: {uploadResult.Exception.Message}");
                    FileLogger.Log($"Stack trace: {uploadResult.Exception.StackTrace}");
                    System.Diagnostics.Debug.WriteLine($"Upload exception: {uploadResult.Exception.Message}");

                    return new UploadResult
                    {
                        Success = false,
                        Error = $"Upload failed: {uploadResult.Exception.Message}"
                    };
                }

                FileLogger.Log($"Upload failed: {uploadResult.Status}");
                System.Diagnostics.Debug.WriteLine($"Upload failed: {uploadResult.Status}");
                return new UploadResult
                {
                    Success = false,
                    Error = $"Upload failed with status: {uploadResult.Status}"
                };
            }
            catch (Exception googleEx) when (googleEx.GetType().Name.Contains("GoogleApi") || (googleEx as dynamic)?.HttpStatusCode != null)
            {
                var httpStatus = (googleEx as dynamic)?.HttpStatusCode as int?;

                FileLogger.Log($"Google API Exception: {googleEx.Message}");
                FileLogger.Log($"HTTP Status: {httpStatus}");
                FileLogger.Log($"Stack trace: {googleEx.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Google API Exception: {googleEx.Message}");

                var error = Models.UploadError.FromException(googleEx, httpStatus);

                // Handle specific error cases
                if (httpStatus == 401 || httpStatus == 403)
                {
                    return new UploadResult
                    {
                        Success = false,
                        Error = error.GetUserMessage()
                    };
                }

                if (httpStatus == 429)
                {
                    return new UploadResult
                    {
                        Success = false,
                        Error = error.GetUserMessage()
                    };
                }

                return new UploadResult
                {
                    Success = false,
                    Error = $"Google API Error ({httpStatus}): {googleEx.Message}"
                };
            }
            catch (IOException ioEx) when (ioEx.Message.Contains("disk space", StringComparison.OrdinalIgnoreCase))
            {
                FileLogger.Log($"Disk space error: {ioEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Disk space error: {ioEx.Message}");

                return new UploadResult
                {
                    Success = false,
                    Error = "Not enough free disk space to prepare this file for upload."
                };
            }
            catch (IOException ioEx)
            {
                FileLogger.Log($"IO Exception: {ioEx.Message}");
                FileLogger.Log($"Stack trace: {ioEx.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"IO Exception: {ioEx.Message}");

                return new UploadResult
                {
                    Success = false,
                    Error = $"File access error: {ioEx.Message}"
                };
            }
            catch (Exception ex)
            {
                FileLogger.Log($"General Exception: {ex.Message}");
                FileLogger.Log($"Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"General Exception: {ex.Message}");

                return new UploadResult
                {
                    Success = false,
                    Error = $"Upload failed: {ex.Message}"
                };
            }
        }
    }
}

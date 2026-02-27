using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using CameraCopyTool.Models;

namespace CameraCopyTool.Services
{
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
        /// Uploads a file to Google Drive.
        /// </summary>
        /// <param name="filePath">Path to the file to upload.</param>
        /// <param name="progress">Progress reporter for upload progress (percentage, speed, time remaining).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Upload result with file ID and status, or null if failed.</returns>
        Task<UploadResult?> UploadFileAsync(
            string filePath,
            IProgress<UploadProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of IGoogleDriveService using Google Drive API v3.
    /// </summary>
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly GoogleDriveSettings _settings;
        private UserCredential? _credential;
        private DriveService? _driveService;

        public GoogleDriveService(GoogleDriveSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
                if (File.Exists(_settings.CredentialsPath))
                {
                    File.Delete(_settings.CredentialsPath);
                }

                _credential = null;
                _driveService = null;
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
            if (!IsAuthenticated || _driveService == null)
            {
                throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
            }

            try
            {
                var fileName = Path.GetFileName(filePath);
                var fileSize = new FileInfo(filePath).Length;
                
                FileLogger.Log($"Starting upload: {fileName} ({fileSize} bytes)");
                System.Diagnostics.Debug.WriteLine($"Starting upload: {fileName} ({fileSize} bytes)");

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

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
                    var uploadResult = await uploadRequest.UploadAsync(CancellationToken.None);
                    
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
                catch (Exception ex) when (ex.GetType().Name.Contains("GoogleApi"))
                {
                    var googleEx = ex as dynamic;
                    var httpStatus = googleEx?.HttpStatusCode ?? "Unknown";
                    
                    FileLogger.Log($"Google API Exception: {ex.Message}");
                    FileLogger.Log($"HTTP Status: {httpStatus}");
                    FileLogger.Log($"Stack trace: {ex.StackTrace}");
                    System.Diagnostics.Debug.WriteLine($"Google API Exception: {ex.Message}");
                    
                    return new UploadResult
                    {
                        Success = false,
                        Error = $"Google API Error ({httpStatus}): {ex.Message}\n\n" +
                               $"Common causes:\n" +
                               $"• Google Drive API not enabled in Google Cloud Console\n" +
                               $"• OAuth consent screen not configured\n" +
                               $"• Invalid OAuth credentials"
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
            catch (TaskCanceledException)
            {
                return new UploadResult
                {
                    Success = false,
                    Error = "Upload was cancelled by the user"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload failed: {ex.Message}");
                return new UploadResult
                {
                    Success = false,
                    Error = $"Upload failed: {ex.Message}"
                };
            }
        }
    }
}

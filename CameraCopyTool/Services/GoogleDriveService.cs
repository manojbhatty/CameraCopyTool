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
        /// <param name="progress">Progress reporter for upload progress.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Google Drive file ID if successful, null otherwise.</returns>
        Task<string?> UploadFileAsync(
            string filePath,
            IProgress<double>? progress = null,
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

        public string? UserEmail => _credential?.UserId;

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
        public async Task<string?> UploadFileAsync(
            string filePath,
            IProgress<double>? progress = null,
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

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName
                };

                var uploadRequest = _driveService.Files.Create(fileMetadata, fileStream, "application/octet-stream");
                uploadRequest.Fields = "id";

                // Track progress
                var lastProgressValue = -1.0;
                uploadRequest.ProgressChanged += (uploadProgress) =>
                {
                    if (progress != null && uploadProgress.BytesSent > 0)
                    {
                        var percentage = (double)uploadProgress.BytesSent / fileSize * 100;
                        var roundedPercentage = Math.Round(percentage, 2);

                        // Only report if changed to avoid too many updates
                        if (Math.Abs(roundedPercentage - lastProgressValue) >= 1.0)
                        {
                            progress.Report(roundedPercentage);
                            lastProgressValue = roundedPercentage;
                        }
                    }
                };

                Google.Apis.Drive.v3.Data.File? uploadedFile = null;
                var uploadResult = await uploadRequest.UploadAsync(cancellationToken);

                if (uploadResult.Status == Google.Apis.Upload.UploadStatus.Completed)
                {
                    uploadedFile = uploadRequest.ResponseBody;
                    progress?.Report(100.0);
                    return uploadedFile?.Id;
                }

                return null;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload failed: {ex.Message}");
                return null;
            }
        }
    }
}

namespace CameraCopyTool.Models
{
    /// <summary>
    /// Configuration settings for Google Drive integration.
    /// All settings must be configured in App.config - no hardcoded defaults.
    /// </summary>
    public class GoogleDriveSettings
    {
        /// <summary>
        /// Gets or sets the OAuth 2.0 scope for Google Drive access.
        /// Must be configured in App.config (GoogleDrive.Scope).
        /// </summary>
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the credentials file name.
        /// Must be configured in App.config (GoogleDrive.CredentialsFileName).
        /// </summary>
        public string CredentialsFileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the application name for Google API calls.
        /// Must be configured in App.config (GoogleDrive.ApplicationName).
        /// </summary>
        public string ApplicationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the OAuth 2.0 Client ID.
        /// Must be configured in App.config (GoogleDrive.ClientId).
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Gets or sets the OAuth 2.0 Client Secret.
        /// Must be configured in App.config (GoogleDrive.ClientSecret).
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Gets the full path to the credentials file.
        /// Stored in %APPDATA%\CameraCopyTool\
        /// </summary>
        public string CredentialsPath => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CameraCopyTool",
            CredentialsFileName);

        /// <summary>
        /// Gets a value indicating whether all required settings are configured.
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Scope) &&
                                     !string.IsNullOrWhiteSpace(CredentialsFileName) &&
                                     !string.IsNullOrWhiteSpace(ApplicationName) &&
                                     !string.IsNullOrWhiteSpace(ClientId) &&
                                     !string.IsNullOrWhiteSpace(ClientSecret);
    }
}

using System.Configuration;
using CameraCopyTool.Models;

namespace CameraCopyTool.Services
{
    /// <summary>
    /// Service for loading application settings from configuration files.
    /// </summary>
    public interface ISettingsLoader
    {
        /// <summary>
        /// Loads Google Drive settings from App.config.
        /// </summary>
        /// <returns>Google Drive settings.</returns>
        GoogleDriveSettings LoadGoogleDriveSettings();
    }

    /// <summary>
    /// Implementation of ISettingsLoader that reads from App.config.
    /// </summary>
    public class AppSettingsLoader : ISettingsLoader
    {
        /// <summary>
        /// Loads Google Drive settings from App.config appSettings.
        /// </summary>
        public GoogleDriveSettings LoadGoogleDriveSettings()
        {
            return new GoogleDriveSettings
            {
                Scope = ConfigurationManager.AppSettings["GoogleDrive.Scope"] 
                        ?? "https://www.googleapis.com/auth/drive.file",
                
                CredentialsFileName = ConfigurationManager.AppSettings["GoogleDrive.CredentialsFileName"] 
                                      ?? "google-drive-credentials.json",
                
                ApplicationName = ConfigurationManager.AppSettings["GoogleDrive.ApplicationName"] 
                                  ?? "CameraCopyTool",
                
                ClientId = ConfigurationManager.AppSettings["GoogleDrive.ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["GoogleDrive.ClientSecret"]
            };
        }
    }
}

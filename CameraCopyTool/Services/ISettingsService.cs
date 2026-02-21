namespace CameraCopyTool.Services;

/// <summary>
/// Service interface for application settings to enable testability.
/// Defines the contract for accessing and persisting user preferences and application state.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets or sets the last used source folder path.
    /// </summary>
    string? LastSourceFolder { get; set; }

    /// <summary>
    /// Gets or sets the last used destination folder path.
    /// </summary>
    string? LastDestinationFolder { get; set; }

    /// <summary>
    /// Gets or sets the user's preferred font size for UI elements.
    /// </summary>
    double FontSize { get; set; }

    /// <summary>
    /// Saves all pending settings changes to persistent storage.
    /// </summary>
    void Save();
}

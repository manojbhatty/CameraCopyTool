namespace CameraCopyTool.Services;

/// <summary>
/// Implementation of settings service using application settings.
/// Provides persistent storage for user preferences and application state.
/// Settings are saved to the user's application data folder and persist between sessions.
/// </summary>
public class SettingsService : ISettingsService
{
    /// <summary>
    /// Gets or sets the last used source folder path.
    /// This path is restored when the application starts.
    /// </summary>
    public string? LastSourceFolder
    {
        get => Properties.Settings.Default.LastSourceFolder;
        set => Properties.Settings.Default.LastSourceFolder = value;
    }

    /// <summary>
    /// Gets or sets the last used destination folder path.
    /// This path is restored when the application starts.
    /// </summary>
    public string? LastDestinationFolder
    {
        get => Properties.Settings.Default.LastDestinationFolder;
        set => Properties.Settings.Default.LastDestinationFolder = value;
    }

    /// <summary>
    /// Gets or sets the user's preferred font size for UI elements.
    /// Valid range is 14-28 pixels.
    /// Default value is 20 pixels for better readability by elderly users.
    /// </summary>
    public double FontSize
    {
        get => Properties.Settings.Default.FontSize;
        set => Properties.Settings.Default.FontSize = value;
    }

    /// <summary>
    /// Saves all pending settings changes to persistent storage.
    /// Should be called when the application closes or when settings are modified.
    /// </summary>
    public void Save()
    {
        Properties.Settings.Default.Save();
    }
}

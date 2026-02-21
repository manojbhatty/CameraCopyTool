using System.IO;
using System.Windows;

namespace CameraCopyTool.Services;

/// <summary>
/// Service interface for dialog operations to enable testability.
/// Defines the contract for user interface dialog operations used throughout the application.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a folder picker dialog.
    /// </summary>
    /// <param name="initialPath">The initial directory to display in the picker.</param>
    /// <returns>The selected folder path, or null if cancelled.</returns>
    string? PickFolder(string? initialPath = null);

    /// <summary>
    /// Shows a message box.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The title bar text.</param>
    /// <param name="buttons">The buttons to display.</param>
    /// <param name="image">The icon to display.</param>
    /// <returns>The MessageBoxResult indicating which button was clicked.</returns>
    MessageBoxResult ShowMessage(string message, string title = "", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None);

    /// <summary>
    /// Shows a confirmation dialog for deleting files.
    /// Displays a clear warning message with permanent deletion notice.
    /// </summary>
    /// <param name="message">The warning message to display.</param>
    /// <param name="fontSize">The font size to use for accessibility.</param>
    /// <returns>True if user confirmed deletion, false otherwise.</returns>
    bool ShowDeleteConfirmation(string message, double fontSize = 14);

    /// <summary>
    /// Shows a confirmation dialog for overwriting a file.
    /// Displays file comparison details to help the user decide.
    /// </summary>
    /// <param name="fileName">The name of the conflicting file.</param>
    /// <param name="sourceInfo">Source file information.</param>
    /// <param name="destInfo">Destination file information.</param>
    /// <returns>The user's overwrite choice.</returns>
    OverwriteChoice ShowOverwriteDialog(string fileName, FileInfo sourceInfo, FileInfo destInfo);
}

/// <summary>
/// Result of the overwrite confirmation dialog.
/// Represents the user's choice when a file conflict is detected.
/// </summary>
public enum OverwriteChoice
{
    /// <summary>
    /// Overwrite the existing file with the source file.
    /// </summary>
    Yes,

    /// <summary>
    /// Skip this file (do not overwrite).
    /// </summary>
    No,

    /// <summary>
    /// Cancel the entire operation (stop processing remaining files).
    /// </summary>
    Cancel
}

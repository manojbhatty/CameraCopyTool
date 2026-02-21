using System.IO;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace CameraCopyTool.Services;

/// <summary>
/// Implementation of dialog operations service.
/// Provides user interface dialogs for folder selection, messages, and confirmations.
/// This service abstracts UI dialog operations to enable unit testing and separation of concerns.
/// </summary>
public class DialogService : IDialogService
{
    /// <summary>
    /// Shows a folder picker dialog and returns the selected folder path.
    /// Uses the Windows API Code Pack CommonOpenFileDialog for a modern folder picker experience.
    /// </summary>
    /// <param name="initialPath">The initial directory to display in the picker. If null or invalid, uses the default location.</param>
    /// <returns>The selected folder path, or null if the user cancelled the dialog.</returns>
    public string? PickFolder(string? initialPath = null)
    {
        var dlg = new CommonOpenFileDialog { IsFolderPicker = true };

        if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
            dlg.InitialDirectory = initialPath;

        return dlg.ShowDialog() == CommonFileDialogResult.Ok ? dlg.FileName : null;
    }

    /// <summary>
    /// Shows a message box with the specified message and options.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The title bar text for the message box.</param>
    /// <param name="buttons">The buttons to display (default: OK).</param>
    /// <param name="image">The icon to display (default: none).</param>
    /// <returns>The <see cref="MessageBoxResult"/> indicating which button was clicked.</returns>
    public MessageBoxResult ShowMessage(string message, string title = "", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        return MessageBox.Show(message, title, buttons, image);
    }

    /// <summary>
    /// Shows a confirmation dialog for overwriting an existing file.
    /// Displays file size and modification date for both source and destination files
    /// to help the user make an informed decision.
    /// </summary>
    /// <param name="fileName">The name of the file that already exists.</param>
    /// <param name="sourceInfo">Information about the source file.</param>
    /// <param name="destInfo">Information about the existing destination file.</param>
    /// <returns>The user's choice: Yes (overwrite), No (skip), or Cancel (stop all).</returns>
    public OverwriteChoice ShowOverwriteDialog(string fileName, FileInfo sourceInfo, FileInfo destInfo)
    {
        var result = MessageBox.Show(
            $"File '{fileName}' already exists.\n\n" +
            $"Source: {sourceInfo.Length / 1024} KB, {sourceInfo.LastWriteTime}\n" +
            $"Destination: {destInfo.Length / 1024} KB, {destInfo.LastWriteTime}\n\n" +
            "Do you want to overwrite it?",
            "Overwrite file?",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return result switch
        {
            MessageBoxResult.Yes => OverwriteChoice.Yes,
            MessageBoxResult.No => OverwriteChoice.No,
            _ => OverwriteChoice.Cancel
        };
    }
}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CameraCopyTool.Commands;
using CameraCopyTool.Models;
using CameraCopyTool.Services;

namespace CameraCopyTool.ViewModels;

/// <summary>
/// Main ViewModel for the CameraCopyTool application.
/// Implements the MVVM (Model-View-ViewModel) pattern with dependency injection for services.
/// This ViewModel manages the core functionality of copying files from a camera/source device
/// to a destination folder on the computer, including file listing, selection, copying progress,
/// and user preferences like font size.
/// </summary>
public class MainViewModel : ViewModelBase
{
    /// <summary>
    /// Service interface for file operations (copy, delete, enumerate, etc.).
    /// </summary>
    private readonly IFileService _fileService;

    /// <summary>
    /// Service interface for displaying dialogs and messages to the user.
    /// </summary>
    private readonly IDialogService _dialogService;

    /// <summary>
    /// Service interface for loading and saving application settings.
    /// </summary>
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// Backing field for the source path (camera/device folder).
    /// </summary>
    private string _sourcePath = string.Empty;

    /// <summary>
    /// Backing field for the destination path (computer folder).
    /// </summary>
    private string _destinationPath = string.Empty;

    /// <summary>
    /// Backing field indicating whether files are currently being loaded.
    /// Used to prevent duplicate load operations and show loading UI.
    /// </summary>
    private bool _isLoading;

    /// <summary>
    /// Backing field indicating whether files are currently being copied.
    /// Used to prevent duplicate copy operations and update UI state.
    /// </summary>
    private bool _isCopying;

    /// <summary>
    /// Backing field for the current progress value during file copy operations.
    /// Represents bytes copied so far.
    /// </summary>
    private int _progressValue;

    /// <summary>
    /// Backing field for the maximum progress value during file copy operations.
    /// Represents total bytes to copy.
    /// </summary>
    private int _progressMaximum;

    /// <summary>
    /// Backing field for the status message displayed to the user.
    /// Shows current operation status (e.g., "Loading files...", "Copying files...").
    /// </summary>
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Backing field for the UI font size setting.
    /// Allows users to adjust text size for accessibility.
    /// </summary>
    private double _fontSize;

    /// <summary>
    /// Backing field for cancellation token source (currently unused).
    /// Reserved for future cancellation of long-running operations.
    /// </summary>
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="fileService">Service for file operations.</param>
    /// <param name="dialogService">Service for displaying dialogs.</param>
    /// <param name="settingsService">Service for managing application settings.</param>
    public MainViewModel(
        IFileService fileService,
        IDialogService dialogService,
        ISettingsService settingsService)
    {
        _fileService = fileService;
        _dialogService = dialogService;
        _settingsService = settingsService;

        // Initialize observable collections for file lists displayed in the UI
        AlreadyCopiedFiles = new ObservableCollection<FileItem>();
        NewFiles = new ObservableCollection<FileItem>();
        DestinationFiles = new ObservableCollection<FileItem>();

        // Subscribe to collection changes to update header counts
        AlreadyCopiedFiles.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AlreadyCopiedFilesHeader));
        NewFiles.CollectionChanged += (s, e) => OnPropertyChanged(nameof(NewFilesHeader));
        DestinationFiles.CollectionChanged += (s, e) => OnPropertyChanged(nameof(DestinationFilesHeader));

        // Initialize commands that bind to UI elements (buttons, menu items, key bindings)
        BrowseSourceCommand = new RelayCommand(_ => BrowseSource());
        BrowseDestinationCommand = new RelayCommand(_ => BrowseDestination());
        CopyCommand = new AsyncRelayCommand(CopyAsync, _ => CanCopy);
        RefreshCommand = new AsyncRelayCommand(_ => LoadFilesAsync());
        DeleteCommand = new AsyncRelayCommand(DeleteSelectedAsync);
        OpenCommand = new RelayCommand(OpenSelected);
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());

        // Load saved settings from previous session
        _sourcePath = _settingsService.LastSourceFolder ?? string.Empty;
        _destinationPath = _settingsService.LastDestinationFolder ?? string.Empty;
        _fontSize = _settingsService.FontSize > 0 ? _settingsService.FontSize : 20;

        // Automatically load files if paths were previously configured
        // This enables the app to show files immediately on startup
        if (!string.IsNullOrEmpty(_sourcePath) || !string.IsNullOrEmpty(_destinationPath))
        {
            _ = LoadFilesAsync();
        }
    }

    #region Properties

    /// <summary>
    /// Gets or sets the source path (camera/device folder).
    /// When changed, saves the path to settings and triggers a debounced file load.
    /// </summary>
    public string SourcePath
    {
        get => _sourcePath;
        set
        {
            if (SetProperty(ref _sourcePath, value))
            {
                _settingsService.LastSourceFolder = value;
                DebounceLoadFiles();
            }
        }
    }

    /// <summary>
    /// Gets or sets the destination path (computer folder).
    /// When changed, saves the path to settings and triggers a debounced file load.
    /// </summary>
    public string DestinationPath
    {
        get => _destinationPath;
        set
        {
            if (SetProperty(ref _destinationPath, value))
            {
                _settingsService.LastDestinationFolder = value;
                DebounceLoadFiles();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether files are currently being loaded.
    /// Used to show/hide loading indicators and prevent duplicate operations.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether files are currently being copied.
    /// Used to enable/disable the Copy button and show copy progress.
    /// </summary>
    public bool IsCopying
    {
        get => _isCopying;
        set
        {
            if (SetProperty(ref _isCopying, value))
            {
                // Notify the Copy command to re-evaluate its CanExecute condition
                ((AsyncRelayCommand)CopyCommand).RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the current progress value (bytes copied) during file copy operations.
    /// Bound to the ProgressBar Value property in the UI.
    /// </summary>
    public int ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    /// <summary>
    /// Gets or sets the maximum progress value (total bytes) during file copy operations.
    /// Bound to the ProgressBar Maximum property in the UI.
    /// </summary>
    public int ProgressMaximum
    {
        get => _progressMaximum;
        set => SetProperty(ref _progressMaximum, value);
    }

    /// <summary>
    /// Gets or sets the status message displayed to the user.
    /// Shows information about current operations and their results.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets the font size for UI elements.
    /// Allows users to customize text size for better readability.
    /// Values are persisted to settings and range from 14 to 28 pixels.
    /// Default is 20 pixels for better readability by elderly users.
    /// </summary>
    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (SetProperty(ref _fontSize, value))
            {
                _settingsService.FontSize = value;
                OnPropertyChanged(nameof(FontSize));
            }
        }
    }

    /// <summary>
    /// Event raised when the Settings window should be opened.
    /// Handled by the View to display the SettingsWindow dialog.
    /// </summary>
    public event Action? OpenSettingsRequested;

    /// <summary>
    /// Gets the formatted header text for the Already Copied Files group.
    /// Format: "Already copied files (count)"
    /// </summary>
    public string AlreadyCopiedFilesHeader => $"Already copied files ({AlreadyCopiedFiles.Count})";

    /// <summary>
    /// Gets the formatted header text for the New Files group.
    /// Format: "New files (count)"
    /// </summary>
    public string NewFilesHeader => $"New files ({NewFiles.Count})";

    /// <summary>
    /// Gets the formatted header text for the Destination Files group.
    /// Format: "Files in computer (count)"
    /// </summary>
    public string DestinationFilesHeader => $"Files in computer ({DestinationFiles.Count})";

    /// <summary>
    /// Gets the collection of files that have already been copied to the destination.
    /// Displayed in the "Already copied files" ListView.
    /// </summary>
    public ObservableCollection<FileItem> AlreadyCopiedFiles { get; }

    /// <summary>
    /// Gets the collection of new files that need to be copied.
    /// Displayed in the "New files" ListView.
    /// </summary>
    public ObservableCollection<FileItem> NewFiles { get; }

    /// <summary>
    /// Gets the collection of files currently in the destination folder.
    /// Displayed in the "Files in destination" ListView.
    /// </summary>
    public ObservableCollection<FileItem> DestinationFiles { get; }

    /// <summary>
    /// Gets or sets the currently selected file in the New Files list.
    /// (Currently unused - multi-selection is preferred)
    /// </summary>
    public FileItem? SelectedNewFile { get; set; }

    /// <summary>
    /// Gets or sets the currently selected file in the Already Copied list.
    /// (Currently unused - multi-selection is preferred)
    /// </summary>
    public FileItem? SelectedAlreadyCopiedFile { get; set; }

    /// <summary>
    /// Gets or sets the currently selected file in the Destination Files list.
    /// (Currently unused - multi-selection is preferred)
    /// </summary>
    public FileItem? SelectedDestinationFile { get; set; }

    /// <summary>
    /// Gets or sets the list of selected files in the New Files ListView.
    /// Used for batch copy operations.
    /// </summary>
    public IList<FileItem> SelectedNewFiles { get; set; } = new List<FileItem>();

    /// <summary>
    /// Gets or sets the list of selected files in the Already Copied ListView.
    /// Used for batch delete or open operations.
    /// </summary>
    public IList<FileItem> SelectedAlreadyCopiedFiles { get; set; } = new List<FileItem>();

    /// <summary>
    /// Gets or sets the list of selected files in the Destination Files ListView.
    /// Used for batch delete or open operations.
    /// </summary>
    public IList<FileItem> SelectedDestinationFiles { get; set; } = new List<FileItem>();

    #endregion

    #region Commands

    /// <summary>
    /// Command for browsing and selecting the source folder.
    /// Bound to the "Browse" button next to the source path TextBox.
    /// </summary>
    public ICommand BrowseSourceCommand { get; }

    /// <summary>
    /// Command for browsing and selecting the destination folder.
    /// Bound to the "Browse" button next to the destination path TextBox.
    /// </summary>
    public ICommand BrowseDestinationCommand { get; }

    /// <summary>
    /// Command for copying selected files from source to destination.
    /// Bound to the "Copy" button. Only executes when files are selected and not currently copying.
    /// </summary>
    public ICommand CopyCommand { get; }

    /// <summary>
    /// Command for refreshing the file lists.
    /// Bound to the F5 key and the Refresh menu item.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Command for deleting selected files.
    /// Bound to the Delete key. Works on files in any of the three ListViews.
    /// </summary>
    public ICommand DeleteCommand { get; }

    /// <summary>
    /// Command for opening selected files with their default application.
    /// Bound to the context menu "Open" option.
    /// </summary>
    public ICommand OpenCommand { get; }

    /// <summary>
    /// Command for opening the Settings dialog.
    /// Bound to the Tools > Settings menu item.
    /// </summary>
    public ICommand OpenSettingsCommand { get; }

    #endregion

    #region Command Handlers

    /// <summary>
    /// Determines whether the Copy command can execute.
    /// Copy is allowed only when not currently copying, not loading files,
    /// and at least one file is selected.
    /// </summary>
    private bool CanCopy => !IsCopying && !IsLoading && SelectedNewFiles.Count > 0;

    /// <summary>
    /// Opens a folder browser dialog to select the source folder.
    /// Updates the SourcePath property if a valid folder is selected.
    /// </summary>
    private void BrowseSource()
    {
        var path = _dialogService.PickFolder(SourcePath);
        if (!string.IsNullOrEmpty(path))
            SourcePath = path;
    }

    /// <summary>
    /// Opens a folder browser dialog to select the destination folder.
    /// Updates the DestinationPath property if a valid folder is selected.
    /// </summary>
    private void BrowseDestination()
    {
        var path = _dialogService.PickFolder(DestinationPath);
        if (!string.IsNullOrEmpty(path))
            DestinationPath = path;
    }

    /// <summary>
    /// Raises the OpenSettingsRequested event to open the Settings dialog.
    /// The View handles this event by creating and showing the SettingsWindow.
    /// </summary>
    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke();
    }

    /// <summary>
    /// Backing field for the debounce cancellation token source.
    /// Used to cancel previous pending file load operations when the path changes rapidly.
    /// </summary>
    private CancellationTokenSource? _debounceCts;

    /// <summary>
    /// Debounces file loading to prevent excessive operations when the path changes rapidly.
    /// Waits 300ms after the last path change before loading files.
    /// If the path changes again within 300ms, the previous load is cancelled.
    /// </summary>
    private async void DebounceLoadFiles()
    {
        // Cancel any pending load operation
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            // Wait 300ms before loading
            await Task.Delay(300, token);
            
            // Only load if not cancelled by another path change
            if (!token.IsCancellationRequested)
                await LoadFilesAsync();
        }
        catch (TaskCanceledException)
        {
            // Expected when path changes before delay completes
        }
    }

    /// <summary>
    /// Loads files from the source and destination folders asynchronously.
    /// Compares files to determine which are new and which have already been copied.
    /// Updates the UI collections with the results.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadFilesAsync()
    {
        // Prevent duplicate load operations
        if (IsLoading)
            return;

        IsLoading = true;
        StatusMessage = "Loading files...";

        try
        {
            var alreadyCopied = new List<FileItem>();
            var newFiles = new List<FileItem>();
            var destFileItems = new List<FileItem>();

            // Run file enumeration on a background thread to keep UI responsive
            await Task.Run(() =>
            {
                // Clean up any temporary files from previous failed copy operations
                _fileService.CleanupTempFiles(DestinationPath);

                // Get file lists from both folders
                var sourceFiles = _fileService.GetFiles(SourcePath).ToList();
                var destFiles = _fileService.GetFiles(DestinationPath).ToList();

                // Create a HashSet for efficient lookup of source file names
                var sourceNames = sourceFiles.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Build destination file list with copy status
                foreach (var f in destFiles)
                {
                    destFileItems.Add(new FileItem
                    {
                        Name = f.Name,
                        ModifiedDate = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        IsAlreadyCopied = sourceNames.Contains(f.Name),
                        FileSize = f.Length,
                        FullPath = f.FullName
                    });
                }

                // Categorize source files as new or already copied
                // A file is considered "already copied" if a file with the same name and size exists in destination
                foreach (var src in sourceFiles)
                {
                    bool existsInDest = destFiles.Any(d => d.Name == src.Name && d.Length == src.Length);
                    var fileItem = new FileItem
                    {
                        Name = src.Name,
                        ModifiedDate = src.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        IsAlreadyCopied = existsInDest,
                        FileSize = src.Length,
                        FullPath = src.FullName
                    };

                    if (existsInDest)
                        alreadyCopied.Add(fileItem);
                    else
                        newFiles.Add(fileItem);
                }
            });

            // Update observable collections on the UI thread
            // ObservableCollection must be modified on the thread that created it (UI thread)
            Application.Current.Dispatcher.Invoke(() =>
            {
                AlreadyCopiedFiles.Clear();
                NewFiles.Clear();
                DestinationFiles.Clear();

                foreach (var f in alreadyCopied)
                    AlreadyCopiedFiles.Add(f);

                foreach (var f in newFiles)
                    NewFiles.Add(f);

                foreach (var f in destFileItems)
                    DestinationFiles.Add(f);
            });

            StatusMessage = $"Loaded {NewFiles.Count} new files, {AlreadyCopiedFiles.Count} already copied";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading files: {ex.Message}";
            _dialogService.ShowMessage($"Error loading files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Copies selected files (or all new files if none selected) from source to destination.
    /// Uses a temporary file with .copying extension during transfer, then atomically replaces
    /// the destination file to prevent corruption if the operation is interrupted.
    /// Shows progress and handles overwrite conflicts with a dialog.
    /// </summary>
    /// <param name="parameter">Command parameter (unused).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CopyAsync(object? parameter)
    {
        // Validate that both paths are set
        if (string.IsNullOrEmpty(SourcePath) || string.IsNullOrEmpty(DestinationPath))
        {
            _dialogService.ShowMessage("Source or destination folder does not exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Determine which files to copy (selected files or all new files)
        var filesToCopy = SelectedNewFiles.Count > 0 ? SelectedNewFiles.ToList() : NewFiles.ToList();

        if (filesToCopy.Count == 0)
        {
            _dialogService.ShowMessage("No files selected to copy!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsCopying = true;
        ProgressValue = 0;
        // Set progress maximum to total bytes to copy
        ProgressMaximum = (int)filesToCopy.Sum(f => f.FileSize);
        StatusMessage = "Copying files...";

        try
        {
            int successCount = 0;
            var copiedFiles = new List<FileItem>();

            foreach (var fileItem in filesToCopy)
            {
                string sourcePath = Path.Combine(SourcePath, fileItem.Name);
                string finalDestPath = Path.Combine(DestinationPath, fileItem.Name);
                string tempDestPath = finalDestPath + ".copying";

                try
                {
                    // Check if destination file already exists
                    if (_fileService.FileExists(finalDestPath))
                    {
                        var sourceInfo = new FileInfo(sourcePath);
                        var destInfo = new FileInfo(finalDestPath);

                        // Show overwrite dialog with file comparison details
                        var choice = _dialogService.ShowOverwriteDialog(fileItem.Name, sourceInfo, destInfo);

                        if (choice == OverwriteChoice.Cancel)
                            break; // Stop copying all remaining files

                        if (choice == OverwriteChoice.No)
                            continue; // Skip this file, continue with next
                    }

                    // Clean up any stale temporary files from previous failed operations
                    _fileService.CleanupTempFiles(DestinationPath);

                    // Copy file to temporary location with progress reporting
                    var progress = new Progress<long>(bytesRead =>
                    {
                        ProgressValue = (int)bytesRead;
                    });

                    await _fileService.CopyFileAsync(sourcePath, tempDestPath, progress, CancellationToken.None);

                    // Atomically replace destination file with temporary file
                    // This prevents corruption if the operation is interrupted
                    File.Move(tempDestPath, finalDestPath, overwrite: true);

                    copiedFiles.Add(fileItem);
                    successCount++;
                }
                catch (OperationCanceledException)
                {
                    // Copy operation was cancelled
                    break;
                }
                catch (IOException ioEx) when (!Directory.Exists(SourcePath))
                {
                    // Handle camera disconnection during copy
                    _dialogService.ShowMessage(
                        "Camera was disconnected during copy.\nPlease reconnect and try again.",
                        "Camera Disconnected",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    break;
                }
                catch (Exception ex)
                {
                    // Clean up temporary file on error
                    _fileService.CleanupTempFiles(DestinationPath);
                    _dialogService.ShowMessage(
                        $"Failed to copy {fileItem.Name}:\n{ex.Message}",
                        "Copy Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }

            // Update UI collections to reflect copied files
            foreach (var fileItem in copiedFiles)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    NewFiles.Remove(fileItem);
                    AlreadyCopiedFiles.Add(fileItem);
                });
            }

            ProgressValue = 0;

            if (successCount > 0)
            {
                _dialogService.ShowMessage(
                    $"Copied {successCount} file(s) successfully!",
                    "Copy Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            // Refresh file lists to show updated state
            await LoadFilesAsync();
        }
        finally
        {
            IsCopying = false;
            StatusMessage = "Copy completed";
        }
    }

    /// <summary>
    /// Deletes selected files from any of the three ListViews.
    /// Shows a confirmation dialog before deleting.
    /// </summary>
    /// <param name="parameter">Command parameter. If provided, specifies the folder path.
    /// Otherwise, uses the SourcePath.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DeleteSelectedAsync(object? parameter)
    {
        // Determine which files to delete based on which ListView has selections
        var itemsToDelete = SelectedNewFiles.Count > 0 ? SelectedNewFiles.ToList() :
                           SelectedAlreadyCopiedFiles.Count > 0 ? SelectedAlreadyCopiedFiles.ToList() :
                           SelectedDestinationFiles.Count > 0 ? SelectedDestinationFiles.ToList() :
                           new List<FileItem>();

        if (itemsToDelete.Count == 0)
            return;

        // Show confirmation dialog
        if (_dialogService.ShowMessage(
                $"Are you sure you want to delete {itemsToDelete.Count} file(s)?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        // Determine the folder path for deletion
        string folderPath = parameter as string ?? SourcePath;

        foreach (var fileItem in itemsToDelete)
        {
            string filePath = Path.Combine(folderPath, fileItem.Name);

            try
            {
                _fileService.DeleteFile(filePath);

                // Remove from UI collection on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (NewFiles.Contains(fileItem))
                        NewFiles.Remove(fileItem);
                    else if (AlreadyCopiedFiles.Contains(fileItem))
                        AlreadyCopiedFiles.Remove(fileItem);
                    else if (DestinationFiles.Contains(fileItem))
                        DestinationFiles.Remove(fileItem);
                });
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Failed to delete {fileItem.Name}:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Refresh file lists after deletion
        await LoadFilesAsync();
    }

    /// <summary>
    /// Opens selected files using their default associated application.
    /// Works on files from any of the three ListViews.
    /// </summary>
    /// <param name="parameter">Command parameter (unused).</param>
    private void OpenSelected(object? parameter)
    {
        // Determine which files to open based on which ListView has selections
        var itemsToOpen = SelectedNewFiles.Count > 0 ? SelectedNewFiles.ToList() :
                         SelectedAlreadyCopiedFiles.Count > 0 ? SelectedAlreadyCopiedFiles.ToList() :
                         SelectedDestinationFiles.Count > 0 ? SelectedDestinationFiles.ToList() :
                         new List<FileItem>();

        foreach (var item in itemsToOpen)
        {
            try
            {
                _fileService.OpenFile(item.FullPath);
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Cannot open file {item.Name}:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Called when the application or window is closing.
    /// Saves current settings and cleans up resources.
    /// </summary>
    public void OnClosing()
    {
        _settingsService.Save();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }

    #endregion
}

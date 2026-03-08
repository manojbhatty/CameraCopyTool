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
    /// Service interface for Google Drive operations.
    /// </summary>
    private readonly IGoogleDriveService _googleDriveService;

    /// <summary>
    /// Service interface for upload history tracking.
    /// </summary>
    private readonly IUploadHistoryService _uploadHistoryService;

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
    /// Backing fields for recent folder history.
    /// </summary>
    private ObservableCollection<string> _recentSourceFolders = new();
    private ObservableCollection<string> _recentDestinationFolders = new();

    /// <summary>
    /// Backing field for help panel visibility.
    /// </summary>
    private bool _showHelpPanel = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="fileService">Service for file operations.</param>
    /// <param name="dialogService">Service for displaying dialogs.</param>
    /// <param name="settingsService">Service for managing application settings.</param>
    /// <param name="googleDriveService">Service for Google Drive operations.</param>
    /// <param name="uploadHistoryService">Service for upload history tracking.</param>
    public MainViewModel(
        IFileService fileService,
        IDialogService dialogService,
        ISettingsService settingsService,
        IGoogleDriveService googleDriveService,
        IUploadHistoryService uploadHistoryService)
    {
        _fileService = fileService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        _googleDriveService = googleDriveService;
        _uploadHistoryService = uploadHistoryService;

        // Initialize observable collections for file lists displayed in the UI
        AlreadyCopiedFiles = new ObservableCollection<FileItem>();
        NewFiles = new ObservableCollection<FileItem>();
        DestinationFiles = new ObservableCollection<FileItem>();

        // Subscribe to collection changes to update header counts and status bar
        AlreadyCopiedFiles.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(AlreadyCopiedFilesHeader));
            OnPropertyChanged(nameof(SourceFileCountText));
        };
        NewFiles.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(NewFilesHeader));
            OnPropertyChanged(nameof(SourceFileCountText));
            OnPropertyChanged(nameof(NewFileCountText));
        };
        DestinationFiles.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(DestinationFilesHeader));
        };

        // Initialize commands that bind to UI elements (buttons, menu items, key bindings)
        BrowseSourceCommand = new RelayCommand(_ => BrowseSource());
        BrowseDestinationCommand = new RelayCommand(_ => BrowseDestination());
        CopyCommand = new AsyncRelayCommand(CopyAsync, _ => CanCopy);
        RefreshCommand = new AsyncRelayCommand(_ => LoadFilesAsync());
        DeleteCommand = new AsyncRelayCommand(DeleteSelectedAsync);
        OpenCommand = new RelayCommand(OpenSelected);
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
        ToggleHelpCommand = new RelayCommand(_ => ToggleHelpPanel());
        LogoutGoogleDriveCommand = new RelayCommand(_ => LogoutGoogleDrive());
        LogoutCommand = new RelayCommand(_ => LogoutWithConfirmation());

        // Load saved settings from previous session
        _sourcePath = _settingsService.LastSourceFolder ?? string.Empty;
        _destinationPath = _settingsService.LastDestinationFolder ?? string.Empty;
        _fontSize = _settingsService.FontSize > 0 ? _settingsService.FontSize : 20;

        // Initialize recent folders lists
        _recentSourceFolders = new ObservableCollection<string>();
        _recentDestinationFolders = new ObservableCollection<string>();

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
    /// Also adds the path to recent folders history.
    /// </summary>
    public string SourcePath
    {
        get => _sourcePath;
        set
        {
            if (SetProperty(ref _sourcePath, value))
            {
                _settingsService.LastSourceFolder = value;
                AddToRecentSourceFolders(value);
                DebounceLoadFiles();
            }
        }
    }

    /// <summary>
    /// Gets or sets the destination path (computer folder).
    /// When changed, saves the path to settings and triggers a debounced file load.
    /// Also adds the path to recent folders history.
    /// </summary>
    public string DestinationPath
    {
        get => _destinationPath;
        set
        {
            if (SetProperty(ref _destinationPath, value))
            {
                _settingsService.LastDestinationFolder = value;
                AddToRecentDestinationFolders(value);
                DebounceLoadFiles();
            }
        }
    }

    /// <summary>
    /// Gets the list of recently used source folders for the ComboBox dropdown.
    /// </summary>
    public ObservableCollection<string> RecentSourceFolders => _recentSourceFolders;

    /// <summary>
    /// Gets the list of recently used destination folders for the ComboBox dropdown.
    /// </summary>
    public ObservableCollection<string> RecentDestinationFolders => _recentDestinationFolders;

    /// <summary>
    /// Gets or sets a value indicating whether files are currently being loaded.
    /// Used to show/hide loading indicators and prevent duplicate operations.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(StatusBarIcon));
                OnPropertyChanged(nameof(StatusBarText));
            }
        }
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
                OnPropertyChanged(nameof(StatusBarIcon));
                OnPropertyChanged(nameof(StatusBarText));
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
        set
        {
            if (SetProperty(ref _progressValue, value))
            {
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum progress value (total bytes) during file copy operations.
    /// Bound to the ProgressBar Maximum property in the UI.
    /// </summary>
    public int ProgressMaximum
    {
        get => _progressMaximum;
        set
        {
            if (SetProperty(ref _progressMaximum, value))
            {
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }
    }

    /// <summary>
    /// Gets the progress percentage for display in the UI.
    /// Calculated from ProgressValue and ProgressMaximum.
    /// </summary>
    public double ProgressPercentage
    {
        get
        {
            if (ProgressMaximum <= 0) return 0;
            return (double)ProgressValue / ProgressMaximum * 100;
        }
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
    /// Gets a value indicating whether Google Drive authentication is available.
    /// </summary>
    public bool IsGoogleDriveConfigured => _googleDriveService != null;

    /// <summary>
    /// Gets a value indicating whether the user is authenticated with Google Drive.
    /// </summary>
    public bool IsGoogleDriveAuthenticated => _googleDriveService?.IsAuthenticated == true;

    /// <summary>
    /// Gets the Google Drive authentication status message.
    /// </summary>
    public string GoogleDriveStatus => _googleDriveService?.IsAuthenticated == true
        ? "Connected to Google Drive"
        : "Not connected to Google Drive";

    /// <summary>
    /// Gets the authenticated Google Drive user's email address.
    /// </summary>
    public string? GoogleDriveUserEmail => _googleDriveService?.UserEmail;

    /// <summary>
    /// Gets the status bar icon (emoji) based on current state.
    /// </summary>
    public string StatusBarIcon
    {
        get
        {
            if (IsLoading) return "⏳";
            if (IsCopying) return "📋";
            if (string.IsNullOrEmpty(SourcePath)) return "📁";
            return "✓";
        }
    }

    /// <summary>
    /// Gets the status bar text based on current state.
    /// </summary>
    public string StatusBarText
    {
        get
        {
            if (IsLoading) return "Loading files...";
            if (IsCopying) return "Copying files...";
            if (string.IsNullOrEmpty(SourcePath)) return "Select source folder";
            return "Ready";
        }
    }

    /// <summary>
    /// Gets the source file count text for the status bar.
    /// Counts only video files.
    /// </summary>
    public string SourceFileCountText
    {
        get
        {
            int totalVideos = AlreadyCopiedFiles.Count(f => _fileService.IsVideoFile(f.Name)) 
                            + NewFiles.Count(f => _fileService.IsVideoFile(f.Name));
            return totalVideos > 0 ? $"{totalVideos} video{(totalVideos != 1 ? "s" : "")} in source" : "No videos in source";
        }
    }

    /// <summary>
    /// Gets the new file count text for the status bar.
    /// Counts only video files.
    /// </summary>
    public string NewFileCountText
    {
        get
        {
            int newCount = NewFiles.Count(f => _fileService.IsVideoFile(f.Name));
            return newCount > 0 ? $"{newCount} new video{(newCount != 1 ? "s" : "")}" : "0 new videos";
        }
    }

    /// <summary>
    /// Event raised when the Settings window should be opened.
    /// Handled by the View to display the SettingsWindow dialog.
    /// </summary>
    public event Action? OpenSettingsRequested;

    /// <summary>
    /// Gets the formatted header text for the Already Copied Files group.
    /// Format: "Already copied videos (count)" - counts only video files
    /// </summary>
    public string AlreadyCopiedFilesHeader => $"✅ Already Copied Videos ({AlreadyCopiedFiles.Count(f => _fileService.IsVideoFile(f.Name))})";

    /// <summary>
    /// Gets the formatted header text for the New Files group.
    /// Format: "🆕 New Videos to Copy (count)" - counts only video files
    /// </summary>
    public string NewFilesHeader => $"🆕 New Videos to Copy ({NewFiles.Count(f => _fileService.IsVideoFile(f.Name))})";

    /// <summary>
    /// Gets the formatted header text for the Destination Files group.
    /// Format: "💻 Videos on Your Computer (count)" - counts only video files
    /// </summary>
    public string DestinationFilesHeader => $"💻 Videos on Your Computer ({DestinationFiles.Count(f => _fileService.IsVideoFile(f.Name))})";

    /// <summary>
    /// Gets or sets a value indicating whether the help panel is visible.
    /// </summary>
    public bool ShowHelpPanel
    {
        get => _showHelpPanel;
        set => SetProperty(ref _showHelpPanel, value);
    }

    /// <summary>
    /// Event raised when files have been loaded and sorted.
    /// Allows the View to update sort indicators.
    /// </summary>
    public event Action? FilesLoaded;

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

    /// <summary>
    /// Command for toggling the help panel visibility.
    /// Bound to the "How to Use" button.
    /// </summary>
    public ICommand ToggleHelpCommand { get; }

    /// <summary>
    /// Gets the command to logout from Google Drive.
    /// </summary>
    public ICommand LogoutGoogleDriveCommand { get; }

    /// <summary>
    /// Gets the command to logout from Google Drive with confirmation dialog.
    /// Bound to the logout button in the main window.
    /// </summary>
    public ICommand LogoutCommand { get; }

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
    /// Adds a folder to the recent source folders list.
    /// Keeps only the 10 most recent folders, removes duplicates.
    /// </summary>
    private void AddToRecentSourceFolders(string folder)
    {
        if (string.IsNullOrEmpty(folder)) return;
        
        // Remove if already exists
        if (_recentSourceFolders.Contains(folder))
        {
            _recentSourceFolders.Remove(folder);
        }
        
        // Add to beginning of list
        _recentSourceFolders.Insert(0, folder);
        
        // Keep only 10 most recent
        while (_recentSourceFolders.Count > 10)
        {
            _recentSourceFolders.RemoveAt(_recentSourceFolders.Count - 1);
        }
        
        // Save to in-memory settings
        if (_settingsService is SettingsService ss)
        {
            ss.RecentSourceFolders = _recentSourceFolders.ToList();
        }
        OnPropertyChanged(nameof(RecentSourceFolders));
    }

    /// <summary>
    /// Adds a folder to the recent destination folders list.
    /// Keeps only the 10 most recent folders, removes duplicates.
    /// </summary>
    private void AddToRecentDestinationFolders(string folder)
    {
        if (string.IsNullOrEmpty(folder)) return;
        
        // Remove if already exists
        if (_recentDestinationFolders.Contains(folder))
        {
            _recentDestinationFolders.Remove(folder);
        }
        
        // Add to beginning of list
        _recentDestinationFolders.Insert(0, folder);
        
        // Keep only 10 most recent
        while (_recentDestinationFolders.Count > 10)
        {
            _recentDestinationFolders.RemoveAt(_recentDestinationFolders.Count - 1);
        }
        
        // Save to in-memory settings
        if (_settingsService is SettingsService ds)
        {
            ds.RecentDestinationFolders = _recentDestinationFolders.ToList();
        }
        OnPropertyChanged(nameof(RecentDestinationFolders));
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
    /// Toggles the visibility of the help panel.
    /// </summary>
    private void ToggleHelpPanel()
    {
        ShowHelpPanel = !ShowHelpPanel;
    }

    /// <summary>
    /// Logs out from Google Drive by deleting stored credentials.
    /// </summary>
    private void LogoutGoogleDrive()
    {
        _googleDriveService?.Logout();
        OnPropertyChanged(nameof(GoogleDriveStatus));
        OnPropertyChanged(nameof(GoogleDriveUserEmail));
        OnPropertyChanged(nameof(IsGoogleDriveAuthenticated));

        _dialogService.ShowMessage(
            "Disconnected from Google Drive.\n\nYou will need to sign in again to upload files.",
            "Logged Out",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Shows a confirmation dialog and logs out from Google Drive if confirmed.
    /// </summary>
    private void LogoutWithConfirmation()
    {
        if (_googleDriveService == null)
        {
            return;
        }

        if (!_googleDriveService.IsAuthenticated)
        {
            _dialogService.ShowMessage(
                "You are not currently signed in to Google Drive.",
                "Not Connected",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var userEmail = _googleDriveService.UserEmail ?? "unknown account";
        var message = $"You are currently connected to Google Drive as:\n\n  {userEmail}\n\n" +
                     $"If you log out, you will need to sign in again to upload files.\n\n" +
                     $"Do you want to continue?";

        var result = _dialogService.ShowMessage(
            message,
            "Confirm Logout",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            LogoutGoogleDrive();
        }
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
                        ModifiedDate = FileItem.FormatRelativeDate(f.LastWriteTime),
                        ModifiedDateTime = f.LastWriteTime,
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
                        ModifiedDate = FileItem.FormatRelativeDate(src.LastWriteTime),
                        ModifiedDateTime = src.LastWriteTime,
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
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
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

                    // Update upload status for destination files
                    UpdateUploadStatus();

                    // Apply default sort by ModifiedDate descending
                    ApplyDefaultSort(AlreadyCopiedFiles);
                    ApplyDefaultSort(NewFiles);
                    ApplyDefaultSort(DestinationFiles);

                    // Notify View that files are loaded and sorted (for sort indicator update)
                    FilesLoaded?.Invoke();
                });
            }
            else
            {
                // Test environment - update directly
                AlreadyCopiedFiles.Clear();
                NewFiles.Clear();
                DestinationFiles.Clear();

                foreach (var f in alreadyCopied)
                    AlreadyCopiedFiles.Add(f);

                foreach (var f in newFiles)
                    NewFiles.Add(f);

                foreach (var f in destFileItems)
                    DestinationFiles.Add(f);

                // Update upload status for destination files
                UpdateUploadStatus();

                // Apply default sort by ModifiedDate descending
                ApplyDefaultSort(AlreadyCopiedFiles);
                ApplyDefaultSort(NewFiles);
                ApplyDefaultSort(DestinationFiles);

                // Notify View that files are loaded and sorted (for sort indicator update)
                FilesLoaded?.Invoke();
            }

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
        long totalBytes = filesToCopy.Sum(f => f.FileSize);
        ProgressMaximum = (int)Math.Min(totalBytes, int.MaxValue);
        System.Diagnostics.Debug.WriteLine($"Starting copy: {filesToCopy.Count} files, {totalBytes} bytes, ProgressMaximum={ProgressMaximum}");
        StatusMessage = "Copying files...";

        try
        {
            int successCount = 0;
            var copiedFiles = new List<FileItem>();
            long bytesCopiedBeforeCurrentFile = 0;

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
                        {
                            // Skip this file but add its size to the cumulative progress
                            bytesCopiedBeforeCurrentFile += fileItem.FileSize;
                            // Update progress to reflect skipped file
                            ProgressValue = (int)Math.Min(bytesCopiedBeforeCurrentFile, int.MaxValue);
                            continue;
                        }
                    }

                    // Clean up any stale temporary files from previous failed operations
                    _fileService.CleanupTempFiles(DestinationPath);

                    // Copy file to temporary location with progress reporting
                    // Capture current file's starting position for cumulative progress
                    long currentFileStart = bytesCopiedBeforeCurrentFile;
                    var progress = new Progress<long>(bytesRead =>
                    {
                        // Only report progress up to 95% during copy to reserve room for move operation
                        long cumulativeBytes = currentFileStart + bytesRead;
                        double progressRatio = (double)cumulativeBytes / totalBytes;
                        int progressValue = (int)(progressRatio * 0.95 * ProgressMaximum);
                        System.Diagnostics.Debug.WriteLine($"Progress: {bytesRead} / {fileItem.FileSize} = {progressValue} / {ProgressMaximum} ({progressRatio * 100:F1}%)");
                        ProgressValue = progressValue;
                    });

                    await _fileService.CopyFileAsync(sourcePath, tempDestPath, progress, CancellationToken.None);

                    // Atomically replace destination file with temporary file
                    // This prevents corruption if the operation is interrupted
                    File.Move(tempDestPath, finalDestPath, overwrite: true);

                    copiedFiles.Add(fileItem);
                    successCount++;
                    bytesCopiedBeforeCurrentFile += fileItem.FileSize;
                    
                    // Update progress after file is fully copied and moved
                    double completedRatio = (double)bytesCopiedBeforeCurrentFile / totalBytes;
                    ProgressValue = (int)(completedRatio * 0.95 * ProgressMaximum);
                    System.Diagnostics.Debug.WriteLine($"File complete: {fileItem.Name}, Progress: {ProgressValue} / {ProgressMaximum}");
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

            // All files copied successfully - set progress to 100%
            ProgressValue = ProgressMaximum;
            System.Diagnostics.Debug.WriteLine($"Copy complete: Progress {ProgressValue} / {ProgressMaximum} (100%)");

            // Update UI collections to reflect copied files
            foreach (var fileItem in copiedFiles)
            {
                if (Application.Current != null && Application.Current.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        NewFiles.Remove(fileItem);
                        AlreadyCopiedFiles.Add(fileItem);
                    });
                }
                else
                {
                    // Test environment - update directly
                    NewFiles.Remove(fileItem);
                    AlreadyCopiedFiles.Add(fileItem);
                }
            }

            if (successCount > 0)
            {
                _dialogService.ShowMessage(
                    $"Copied {successCount} file(s) successfully!",
                    "Copy Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            // Reset progress after showing completion message
            ProgressValue = 0;

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
    /// Files in New Files or Already Copied lists are deleted from Source.
    /// Files in Destination list are deleted from Destination.
    /// </summary>
    /// <param name="parameter">Command parameter. If provided, specifies the folder path.
    /// Otherwise, determines path based on which list has selections.</param>
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

        // Determine the folder path for deletion based on which list has selections
        // BDD Rule 3: Delete from New/Already Copied = Source, Delete from Destination = Destination
        string folderPath = parameter as string ??
                           (SelectedDestinationFiles.Count > 0 ? DestinationPath : SourcePath);

        string sourceLabel = folderPath == SourcePath ? "from your camera" : "from your computer";

        // Show confirmation dialog with explicit warning
        string message = $"⚠️ This will PERMANENTLY delete {itemsToDelete.Count} file(s) {sourceLabel}.\n\nThis action cannot be undone.";
        if (!_dialogService.ShowDeleteConfirmation(message, FontSize))
            return;

        foreach (var fileItem in itemsToDelete)
        {
            string filePath = Path.Combine(folderPath, fileItem.Name);

            try
            {
                _fileService.DeleteFile(filePath);

                // Remove from UI collection on the UI thread
                if (Application.Current != null && Application.Current.Dispatcher != null)
                {
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
                else
                {
                    // Test environment - update directly
                    if (NewFiles.Contains(fileItem))
                        NewFiles.Remove(fileItem);
                    else if (AlreadyCopiedFiles.Contains(fileItem))
                        AlreadyCopiedFiles.Remove(fileItem);
                    else if (DestinationFiles.Contains(fileItem))
                        DestinationFiles.Remove(fileItem);
                }
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
    /// Updates the upload status icons for destination files based on upload history.
    /// </summary>
    public void UpdateUploadStatus()
    {
        System.Diagnostics.Debug.WriteLine($"UpdateUploadStatus called, _uploadHistoryService={_uploadHistoryService != null}, DestinationFiles.Count={DestinationFiles.Count}");
        
        if (_uploadHistoryService == null)
        {
            System.Diagnostics.Debug.WriteLine("UpdateUploadStatus: _uploadHistoryService is null");
            return;
        }

        // Update each destination file with upload status
        foreach (var fileItem in DestinationFiles)
        {
            var entry = _uploadHistoryService.GetEntryByFilePath(fileItem.FullPath);
            System.Diagnostics.Debug.WriteLine($"UpdateUploadStatus: Checking file '{fileItem.FullPath}', found entry={entry != null}");
            
            if (entry != null)
            {
                System.Diagnostics.Debug.WriteLine($"  Entry status={entry.Status}, fileName={entry.FileName}");
                
                if (entry.Status == UploadHistoryStatus.LocalFileDeleted)
                {
                    // File was uploaded but local file is now deleted
                    fileItem.UploadStatus = "deleted";
                    fileItem.UploadTooltip = "Local file deleted after upload";
                }
                else if (entry.HasFileChanged())
                {
                    // File has changed since upload
                    fileItem.UploadStatus = "changed";
                    fileItem.UploadTooltip = $"File changed since upload on {entry.Timestamp:yyyy-MM-dd HH:mm:ss}";
                }
                else if (entry.Status == UploadHistoryStatus.Success)
                {
                    // Successfully uploaded
                    fileItem.UploadStatus = "uploaded";
                    fileItem.UploadTooltip = entry.UploadedDateString;
                    System.Diagnostics.Debug.WriteLine($"  Set UploadStatus='uploaded' for {fileItem.Name}");
                }
            }
            else
            {
                // No upload history
                fileItem.UploadStatus = null;
                fileItem.UploadTooltip = null;
            }
        }
    }

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

    /// <summary>
    /// Applies default sort by ModifiedDateTime descending to an ObservableCollection.
    /// </summary>
    private static void ApplyDefaultSort(ObservableCollection<FileItem> collection)
    {
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(collection);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(FileItem.ModifiedDateTime), System.ComponentModel.ListSortDirection.Descending));
    }

    #endregion
}

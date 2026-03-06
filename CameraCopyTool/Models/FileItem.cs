using System.ComponentModel;
using System.IO;

namespace CameraCopyTool.Models;

/// <summary>
/// Represents a file item with metadata for display in the UI.
/// Implements INotifyPropertyChanged to support WPF data binding.
/// Used to display file information in ListView controls with copy status tracking.
/// </summary>
public class FileItem : INotifyPropertyChanged
{
    /// <summary>
    /// List of supported video file extensions.
    /// </summary>
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm", ".m4v", ".mpeg", ".mpg", ".3gp", ".3g2"
    };

    /// <summary>
    /// Backing field for the file name.
    /// </summary>
    private string _name = string.Empty;

    /// <summary>
    /// Backing field for the formatted modified date string.
    /// </summary>
    private string _modifiedDate = string.Empty;

    /// <summary>
    /// Backing field for the actual modified date/time (used for sorting).
    /// </summary>
    private DateTime _modifiedDateTime;

    /// <summary>
    /// Backing field indicating whether this file has already been copied to the destination.
    /// </summary>
    private bool _isAlreadyCopied;

    /// <summary>
    /// Backing field for the upload status to Google Drive.
    /// </summary>
    private string? _uploadStatus;

    /// <summary>
    /// Backing field for the upload tooltip.
    /// </summary>
    private string? _uploadTooltip;

    /// <summary>
    /// Gets or sets the name of the file.
    /// When changed, also notifies that DisplayName has changed.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(IsVideoFile));
            }
        }
    }

    /// <summary>
    /// Gets or sets the formatted last modified date string.
    /// Display format shows relative time for recent files (e.g., "Today, 10:30 AM", "Yesterday, 3:45 PM")
    /// or full date for older files (e.g., "Mar 06, 2026 10:30 PM").
    /// </summary>
    public string ModifiedDate
    {
        get => _modifiedDate;
        set
        {
            if (_modifiedDate != value)
            {
                _modifiedDate = value;
                OnPropertyChanged(nameof(ModifiedDate));
            }
        }
    }

    /// <summary>
    /// Gets or sets the actual modified date/time.
    /// Used for sorting (not displayed directly).
    /// </summary>
    public DateTime ModifiedDateTime
    {
        get => _modifiedDateTime;
        set
        {
            if (_modifiedDateTime != value)
            {
                _modifiedDateTime = value;
                OnPropertyChanged(nameof(ModifiedDateTime));
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this file has already been copied.
    /// When changed, also notifies that DisplayName has changed (for tick icon display).
    /// </summary>
    public bool IsAlreadyCopied
    {
        get => _isAlreadyCopied;
        set
        {
            if (_isAlreadyCopied != value)
            {
                _isAlreadyCopied = value;
                OnPropertyChanged(nameof(IsAlreadyCopied));
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether this file is a video file based on its extension.
    /// </summary>
    public bool IsVideoFile => !string.IsNullOrEmpty(Name) && VideoExtensions.Contains(Path.GetExtension(Name));

    /// <summary>
    /// Gets the display name for the file, including a tick icon (✅) if already copied.
    /// This is a computed property that combines Name and IsAlreadyCopied status.
    /// </summary>
    public string DisplayName => IsAlreadyCopied ? $"✅ {Name}" : Name;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// Used for progress calculation during copy operations.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the full path to the file.
    /// Used for file operations like open, delete, and copy.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the upload status to Google Drive (null, "uploaded", "changed", "deleted").
    /// </summary>
    public string? UploadStatus
    {
        get => _uploadStatus;
        set
        {
            if (_uploadStatus != value)
            {
                _uploadStatus = value;
                OnPropertyChanged(nameof(UploadStatus));
                OnPropertyChanged(nameof(UploadIcon));
                OnPropertyChanged(nameof(UploadIconColor));
            }
        }
    }

    /// <summary>
    /// Gets or sets the tooltip text for the upload status.
    /// </summary>
    public string? UploadTooltip
    {
        get => _uploadTooltip;
        set
        {
            if (_uploadTooltip != value)
            {
                _uploadTooltip = value;
                OnPropertyChanged(nameof(UploadTooltip));
            }
        }
    }

    /// <summary>
    /// Gets the upload status icon based on the upload status.
    /// </summary>
    public string UploadIcon => UploadStatus switch
    {
        "uploaded" => "☁️⬆️",
        "changed" => "⚠️",
        "deleted" => "❌",
        _ => string.Empty
    };

    /// <summary>
    /// Gets the color for the upload status icon.
    /// </summary>
    public string UploadIconColor => UploadStatus switch
    {
        "uploaded" => "#2196F3", // Blue for uploaded
        "changed" => "#FF9800",  // Orange for warning
        "deleted" => "#F44336",  // Red for deleted
        _ => "#808080"
    };

    /// <summary>
    /// Event raised when a property value changes.
    /// Required for INotifyPropertyChanged implementation.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Formats a DateTime into a human-readable relative date string.
    /// Shows "Today" or "Yesterday" for recent files, otherwise shows "Mar 06, 2026 10:30 PM".
    /// </summary>
    public static string FormatRelativeDate(DateTime dateTime)
    {
        var now = DateTime.Now;
        var timeSpan = now - dateTime;

        // Today: Show "Today, 10:30 AM"
        if (dateTime.Date == now.Date)
        {
            return $"Today, {dateTime:h:mm tt}";
        }

        // Yesterday: Show "Yesterday, 3:45 PM"
        if (dateTime.Date == now.AddDays(-1).Date)
        {
            return $"Yesterday, {dateTime:h:mm tt}";
        }

        // Within last 7 days: Show day name "Friday, 10:30 AM"
        if (timeSpan.TotalDays < 7)
        {
            return $"{dateTime:dddd}, {dateTime:h:mm tt}";
        }

        // Older: Show full date "Mar 06, 2026 10:30 PM"
        return $"{dateTime:MMM dd, yyyy} {dateTime:h:mm tt}";
    }
}

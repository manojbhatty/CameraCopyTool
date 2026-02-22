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
    /// Backing field indicating whether this file has already been copied to the destination.
    /// </summary>
    private bool _isAlreadyCopied;

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
    /// Display format is "yyyy-MM-dd HH:mm".
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
}

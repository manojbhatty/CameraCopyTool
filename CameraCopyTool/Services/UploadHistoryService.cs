using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CameraCopyTool.Models;

namespace CameraCopyTool.Services
{
    /// <summary>
    /// Settings for upload history cleanup.
    /// </summary>
    public class UploadHistorySettings
    {
        public int MaxEntries { get; set; }
        public int CleanupGracePeriodDays { get; set; } = 30;
        public int CleanupFrequencyDays { get; set; } = 7;
        public DateTime? LastCleanup { get; set; }

        /// <summary>
        /// Loads settings from user settings with defaults.
        /// </summary>
        public static UploadHistorySettings LoadFromSettings()
        {
            var settings = new UploadHistorySettings();
            
            try
            {
                // Load from user settings (defined in Settings.settings)
                settings.MaxEntries = Properties.Settings.Default.UploadHistoryMaxEntries;
            }
            catch
            {
                settings.MaxEntries = 500; // Default on error
            }
            
            return settings;
        }
    }

    /// <summary>
    /// Service for managing upload history.
    /// </summary>
    public interface IUploadHistoryService
    {
        /// <summary>
        /// Gets all upload history entries.
        /// </summary>
        IReadOnlyList<UploadHistoryEntry> GetAllEntries();

        /// <summary>
        /// Gets the recent upload history entries.
        /// </summary>
        IReadOnlyList<UploadHistoryEntry> GetRecentEntries(int count);

        /// <summary>
        /// Adds an entry to the upload history.
        /// </summary>
        Task AddEntryAsync(UploadHistoryEntry entry);

        /// <summary>
        /// Clears all upload history.
        /// </summary>
        Task ClearHistoryAsync();

        /// <summary>
        /// Deletes an entry from the upload history.
        /// </summary>
        Task DeleteEntryAsync(Guid id);

        /// <summary>
        /// Gets statistics about upload history.
        /// </summary>
        UploadHistoryStatistics GetStatistics();

        /// <summary>
        /// Runs cleanup on old and invalid entries.
        /// </summary>
        void RunCleanup();

        /// <summary>
        /// Gets an entry by file path.
        /// </summary>
        UploadHistoryEntry? GetEntryByFilePath(string filePath);
    }

    /// <summary>
    /// Statistics about upload history.
    /// </summary>
    public class UploadHistoryStatistics
    {
        public int TotalUploads { get; set; }
        public int SuccessfulUploads { get; set; }
        public int FailedUploads { get; set; }
        public int CancelledUploads { get; set; }
        public int SkippedUploads { get; set; }
        public long TotalBytesUploaded { get; set; }
        public double SuccessRate => TotalUploads > 0 ? (double)SuccessfulUploads / TotalUploads * 100 : 0;
        public DateTime? LastUploadDate { get; set; }
    }

    /// <summary>
    /// Implementation of IUploadHistoryService using JSON file storage.
    /// </summary>
    public class UploadHistoryService : IUploadHistoryService
    {
        private readonly string _historyFilePath;
        private readonly List<UploadHistoryEntry> _entries;
        private readonly object _lockObj = new object();
        private UploadHistorySettings _settings;

        public UploadHistoryService()
        {
            // Save in the same folder as the application executable
            var appFolder = AppDomain.CurrentDomain.BaseDirectory;
            _historyFilePath = Path.Combine(appFolder, "upload_history.json");
            _entries = new List<UploadHistoryEntry>();
            
            // Load settings from user settings
            _settings = UploadHistorySettings.LoadFromSettings();

            // Load existing history and settings
            LoadHistory();
            
            // Run cleanup on startup if needed
            RunCleanupIfNeeded();
        }

        public IReadOnlyList<UploadHistoryEntry> GetAllEntries()
        {
            lock (_lockObj)
            {
                return _entries.OrderByDescending(e => e.Timestamp).ToList().AsReadOnly();
            }
        }

        public IReadOnlyList<UploadHistoryEntry> GetRecentEntries(int count)
        {
            lock (_lockObj)
            {
                return _entries.OrderByDescending(e => e.Timestamp).Take(count).ToList().AsReadOnly();
            }
        }

        public async Task AddEntryAsync(UploadHistoryEntry entry)
        {
            FileLogger.Log($"AddEntryAsync called: fileName={entry.FileName}, filePath={entry.FilePath}, status={entry.Status}");
            
            await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    FileLogger.Log($"Adding entry to history (current count: {_entries.Count})");
                    _entries.Add(entry);

                    // Trim old entries if necessary
                    while (_entries.Count > _settings.MaxEntries)
                    {
                        _entries.RemoveAt(0);
                    }

                    SaveHistory();
                    FileLogger.Log($"History saved (new count: {_entries.Count})");
                }
            });
        }

        public async Task ClearHistoryAsync()
        {
            await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    _entries.Clear();
                    SaveHistory();
                }
            });
        }

        public async Task DeleteEntryAsync(Guid id)
        {
            await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    var entry = _entries.FirstOrDefault(e => e.Id == id);
                    if (entry != null)
                    {
                        _entries.Remove(entry);
                        SaveHistory();
                    }
                }
            });
        }

        public UploadHistoryStatistics GetStatistics()
        {
            lock (_lockObj)
            {
                return new UploadHistoryStatistics
                {
                    TotalUploads = _entries.Count,
                    SuccessfulUploads = _entries.Count(e => e.Status == UploadHistoryStatus.Success),
                    FailedUploads = _entries.Count(e => e.Status == UploadHistoryStatus.Failed),
                    CancelledUploads = _entries.Count(e => e.Status == UploadHistoryStatus.Cancelled),
                    SkippedUploads = _entries.Count(e => e.Status == UploadHistoryStatus.Skipped),
                    TotalBytesUploaded = _entries.Where(e => e.Status == UploadHistoryStatus.Success)
                                                  .Sum(e => e.FileSize),
                    LastUploadDate = _entries.Max(e => e.Timestamp)
                };
            }
        }

        public UploadHistoryEntry? GetEntryByFilePath(string filePath)
        {
            lock (_lockObj)
            {
                return _entries.FirstOrDefault(e => e.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void RunCleanup()
        {
            lock (_lockObj)
            {
                var cleanupCount = 0;
                var gracePeriodCutoff = DateTime.Now.AddDays(-_settings.CleanupGracePeriodDays);

                // Remove entries marked for cleanup that are older than grace period
                var entriesToRemove = _entries
                    .Where(e => e.MarkedForCleanup.HasValue && e.MarkedForCleanup.Value < gracePeriodCutoff)
                    .ToList();

                foreach (var entry in entriesToRemove)
                {
                    _entries.Remove(entry);
                    cleanupCount++;
                }

                // Mark missing files for cleanup (skip entries with empty paths)
                foreach (var entry in _entries.ToList())
                {
                    // Skip entries that don't have a valid file path
                    if (string.IsNullOrEmpty(entry.FilePath))
                        continue;
                    
                    if (!File.Exists(entry.FilePath) && !entry.MarkedForCleanup.HasValue)
                    {
                        entry.MarkedForCleanup = DateTime.Now;
                        entry.Status = UploadHistoryStatus.LocalFileDeleted;
                    }
                }

                // Enforce max entries limit (remove oldest first)
                while (_entries.Count > _settings.MaxEntries)
                {
                    var oldest = _entries.OrderBy(e => e.Timestamp).FirstOrDefault();
                    if (oldest != null)
                    {
                        _entries.Remove(oldest);
                        cleanupCount++;
                    }
                }

                // Update last cleanup timestamp
                _settings.LastCleanup = DateTime.Now;

                // Save updated history
                SaveHistory();

                if (cleanupCount > 0)
                {
                    FileLogger.Log($"Cleaned up {cleanupCount} old entries from upload history");
                }
                
                // Clean up old debug log files (older than 30 days)
                CleanupOldLogFiles();
            }
        }

        /// <summary>
        /// Deletes debug log files older than configured retention period.
        /// </summary>
        private void CleanupOldLogFiles()
        {
            try
            {
                var logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                
                if (!Directory.Exists(logFolder))
                    return;
                
                // Get retention days from user settings (default: 30)
                int retentionDays = Properties.Settings.Default.DebugLogRetentionDays;
                
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var oldLogs = Directory.GetFiles(logFolder, "upload-*.log")
                    .Where(f => File.GetCreationTime(f) < cutoffDate)
                    .ToList();
                
                foreach (var logFile in oldLogs)
                {
                    try
                    {
                        File.Delete(logFile);
                        FileLogger.Log($"Deleted old log file: {logFile}");
                    }
                    catch (Exception ex)
                    {
                        FileLogger.Log($"Failed to delete old log file {logFile}: {ex.Message}");
                    }
                }
                
                if (oldLogs.Count > 0)
                {
                    FileLogger.Log($"Cleaned up {oldLogs.Count} old debug log files (older than {retentionDays} days)");
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Failed to cleanup old log files: {ex.Message}");
            }
        }

        private void RunCleanupIfNeeded()
        {
            // Run cleanup if never run before or if last cleanup was more than X days ago
            if (!_settings.LastCleanup.HasValue ||
                _settings.LastCleanup.Value.AddDays(_settings.CleanupFrequencyDays) < DateTime.Now)
            {
                FileLogger.Log("Running scheduled upload history cleanup...");
                RunCleanup();
            }
        }

        private void LoadHistory()
        {
            try
            {
                FileLogger.Log($"LoadHistory: Checking for history file at {_historyFilePath}");
                
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    FileLogger.Log($"LoadHistory: Read JSON ({json.Length} chars)");
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var entries = JsonSerializer.Deserialize<List<UploadHistoryEntry>>(json, options);
                    if (entries != null)
                    {
                        FileLogger.Log($"LoadHistory: Deserialized {entries.Count} entries");
                        foreach (var entry in entries)
                        {
                            FileLogger.Log($"  - Entry: fileName='{entry.FileName}', filePath='{entry.FilePath}', status={entry.Status}");
                        }
                        
                        _entries.Clear();
                        _entries.AddRange(entries);
                        FileLogger.Log($"LoadHistory: Loaded {_entries.Count} entries into memory");
                    }
                }
                else
                {
                    FileLogger.Log("LoadHistory: History file does not exist");
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Failed to load upload history: {ex.Message}");
            }
        }

        private void SaveHistory()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(_entries, options);
                File.WriteAllText(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Failed to save upload history: {ex.Message}");
            }
        }
    }
}

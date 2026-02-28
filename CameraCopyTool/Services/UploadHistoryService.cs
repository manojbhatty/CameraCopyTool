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
        private const int MaxHistoryEntries = 1000;

        public UploadHistoryService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CameraCopyTool");

            Directory.CreateDirectory(appDataPath);
            _historyFilePath = Path.Combine(appDataPath, "upload_history.json");
            _entries = new List<UploadHistoryEntry>();

            // Load existing history
            LoadHistory();
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
            await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    _entries.Add(entry);

                    // Trim old entries if necessary
                    while (_entries.Count > MaxHistoryEntries)
                    {
                        _entries.RemoveAt(0);
                    }

                    SaveHistory();
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

        private void LoadHistory()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    var entries = JsonSerializer.Deserialize<List<UploadHistoryEntry>>(json);
                    if (entries != null)
                    {
                        _entries.Clear();
                        _entries.AddRange(entries);
                    }
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

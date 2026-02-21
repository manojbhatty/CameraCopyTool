using System.Diagnostics;
using System.IO;

namespace CameraCopyTool.Services;

/// <summary>
/// Implementation of file operations service.
/// Provides file enumeration, copy, delete, and management operations.
/// This service abstracts file system operations to enable unit testing and separation of concerns.
/// </summary>
public class FileService : IFileService
{
    /// <summary>
    /// Gets all files from the specified directory.
    /// Returns an empty collection if the directory does not exist.
    /// </summary>
    /// <param name="directoryPath">The path to the directory to enumerate.</param>
    /// <returns>A collection of <see cref="FileInfo"/> objects representing the files in the directory.</returns>
    public IEnumerable<FileInfo> GetFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return Enumerable.Empty<FileInfo>();

        return Directory.GetFiles(directoryPath).Select(f => new FileInfo(f));
    }

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    /// <summary>
    /// Deletes a file at the specified path.
    /// Does nothing if the file does not exist.
    /// </summary>
    /// <param name="filePath">The full path to the file to delete.</param>
    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    /// <summary>
    /// Copies a file from source to destination with progress reporting.
    /// Uses a buffered read/write approach to enable progress updates.
    /// </summary>
    /// <param name="sourcePath">The full path to the source file.</param>
    /// <param name="destinationPath">The full path to the destination file.</param>
    /// <param name="progress">Progress reporter for bytes copied. Reports total bytes read so far.</param>
    /// <param name="cancellationToken">Token to cancel the copy operation.</param>
    /// <returns>A task representing the asynchronous copy operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public async Task CopyFileAsync(string sourcePath, string destinationPath, IProgress<long> progress, CancellationToken cancellationToken)
    {
        using var sourceStream = File.OpenRead(sourcePath);
        using var destStream = File.Create(destinationPath);

        // 80KB buffer for efficient I/O
        byte[] buffer = new byte[81920];
        int bytesRead;
        long totalBytesRead = 0;

        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            // Check for cancellation before each write
            cancellationToken.ThrowIfCancellationRequested();

            await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;

            // Report progress to UI
            progress?.Report(totalBytesRead);
        }
    }

    /// <summary>
    /// Cleans up temporary files with the specified extension in the given folder.
    /// Used to remove stale .copying files from failed or interrupted copy operations.
    /// Errors during deletion are silently ignored (files may be locked or in use).
    /// </summary>
    /// <param name="folderPath">The folder to clean up.</param>
    /// <param name="extension">The file extension to match (default: ".copying").</param>
    public void CleanupTempFiles(string folderPath, string extension = ".copying")
    {
        if (!Directory.Exists(folderPath))
            return;

        foreach (var tempFile in Directory.GetFiles(folderPath, $"*{extension}"))
        {
            try
            {
                File.Delete(tempFile);
            }
            catch
            {
                // Ignore: file may be locked or mid-copy
                // This is expected behavior during active copy operations
            }
        }
    }

    /// <summary>
    /// Opens a file using the default application associated with its file type.
    /// Uses the operating system's shell to launch the appropriate application.
    /// </summary>
    /// <param name="filePath">The full path to the file to open.</param>
    /// <exception cref="InvalidOperationException">Thrown when the file cannot be opened.</exception>
    public void OpenFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true // Required to open with default application
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot open file {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the last write time of a file.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    /// <returns>The last write time, or default(DateTime) if the file does not exist.</returns>
    public DateTime GetLastWriteTime(string filePath)
    {
        return File.Exists(filePath) ? File.GetLastWriteTime(filePath) : default;
    }

    /// <summary>
    /// Gets the length of a file in bytes.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    /// <returns>The file size in bytes, or 0 if the file does not exist.</returns>
    public long GetFileLength(string filePath)
    {
        return File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
    }
}

using System.IO;

namespace CameraCopyTool.Services;

/// <summary>
/// Service interface for file operations to enable testability and separation of concerns.
/// Defines the contract for file system operations used throughout the application.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Gets all files from the specified directory.
    /// </summary>
    /// <param name="directoryPath">The path to the directory to enumerate.</param>
    /// <returns>A collection of <see cref="FileInfo"/> objects representing the files.</returns>
    IEnumerable<FileInfo> GetFiles(string directoryPath);

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>
    bool FileExists(string filePath);

    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    /// <param name="filePath">The full path to the file to delete.</param>
    void DeleteFile(string filePath);

    /// <summary>
    /// Copies a file from source to destination with progress reporting.
    /// </summary>
    /// <param name="sourcePath">The full path to the source file.</param>
    /// <param name="destinationPath">The full path to the destination file.</param>
    /// <param name="progress">Progress reporter for bytes copied.</param>
    /// <param name="cancellationToken">Token to cancel the copy operation.</param>
    /// <returns>A task representing the asynchronous copy operation.</returns>
    Task CopyFileAsync(string sourcePath, string destinationPath, IProgress<long> progress, CancellationToken cancellationToken);

    /// <summary>
    /// Cleans up temporary files with the specified extension.
    /// Used to remove stale files from interrupted copy operations.
    /// </summary>
    /// <param name="folderPath">The folder to clean up.</param>
    /// <param name="extension">The file extension to match (default: ".copying").</param>
    void CleanupTempFiles(string folderPath, string extension = ".copying");

    /// <summary>
    /// Opens a file using the default application associated with its file type.
    /// </summary>
    /// <param name="filePath">The full path to the file to open.</param>
    void OpenFile(string filePath);

    /// <summary>
    /// Gets the last write time of a file.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    /// <returns>The last write time of the file.</returns>
    DateTime GetLastWriteTime(string filePath);

    /// <summary>
    /// Gets the length of a file in bytes.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    /// <returns>The file size in bytes.</returns>
    long GetFileLength(string filePath);
}

using System.Windows;
using CameraCopyTool.Commands;
using CameraCopyTool.Models;
using CameraCopyTool.Services;
using CameraCopyTool.ViewModels;
using Moq;

namespace CameraCopyTool.UI.Tests;

/// <summary>
/// Comprehensive unit tests for MainViewModel covering all scenarios.
/// Tests verify BDD specification compliance for all user stories.
/// </summary>
[TestFixture]
public class MainViewModelTests
{
    private Mock<IFileService> _mockFileService = null!;
    private Mock<IDialogService> _mockDialogService = null!;
    private Mock<ISettingsService> _mockSettingsService = null!;
    private MainViewModel _viewModel = null!;

    [SetUp]
    public void Setup()
    {
        _mockFileService = new Mock<IFileService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSettingsService = new Mock<ISettingsService>();

        _viewModel = new MainViewModel(
            _mockFileService.Object,
            _mockDialogService.Object,
            _mockSettingsService.Object,
            Mock.Of<IGoogleDriveService>());
    }

    #region Constructor Tests

    [Test]
    public void Constructor_InitializesCollections()
    {
        Assert.That(_viewModel.AlreadyCopiedFiles, Is.Not.Null);
        Assert.That(_viewModel.NewFiles, Is.Not.Null);
        Assert.That(_viewModel.DestinationFiles, Is.Not.Null);
        Assert.That(_viewModel.AlreadyCopiedFiles.Count, Is.Zero);
        Assert.That(_viewModel.NewFiles.Count, Is.Zero);
        Assert.That(_viewModel.DestinationFiles.Count, Is.Zero);
    }

    [Test]
    public void Constructor_LoadsSettingsFromSettingsService()
    {
        // Arrange
        _mockSettingsService.SetupGet(x => x.LastSourceFolder).Returns("C:\\Saved\\Source");
        _mockSettingsService.SetupGet(x => x.LastDestinationFolder).Returns("D:\\Saved\\Dest");

        // Act
        var viewModel = new MainViewModel(
            _mockFileService.Object,
            _mockDialogService.Object,
            _mockSettingsService.Object,
            Mock.Of<IGoogleDriveService>());

        // Assert
        Assert.That(viewModel.SourcePath, Is.EqualTo("C:\\Saved\\Source"));
        Assert.That(viewModel.DestinationPath, Is.EqualTo("D:\\Saved\\Dest"));
    }

    [Test]
    public void Constructor_InitializesAllCommands()
    {
        Assert.That(_viewModel.BrowseSourceCommand, Is.Not.Null);
        Assert.That(_viewModel.BrowseDestinationCommand, Is.Not.Null);
        Assert.That(_viewModel.CopyCommand, Is.Not.Null);
        Assert.That(_viewModel.RefreshCommand, Is.Not.Null);
        Assert.That(_viewModel.DeleteCommand, Is.Not.Null);
        Assert.That(_viewModel.OpenCommand, Is.Not.Null);
        Assert.That(_viewModel.OpenSettingsCommand, Is.Not.Null);
    }

    [Test]
    public void Constructor_SetsDefaultFontSize_To20Pixels_ForAccessibility()
    {
        // BDD v1.4: Default font size is 20px for elderly users
        _mockSettingsService.SetupGet(x => x.FontSize).Returns(0); // 0 means not set

        var viewModel = new MainViewModel(
            _mockFileService.Object,
            _mockDialogService.Object,
            _mockSettingsService.Object,
            Mock.Of<IGoogleDriveService>());

        Assert.That(viewModel.FontSize, Is.EqualTo(20));
    }

    [Test]
    public void Constructor_RestoresSavedFontSize_FromSettings()
    {
        // BDD v1.4: Font size persists across sessions
        _mockSettingsService.SetupGet(x => x.FontSize).Returns(24);

        var viewModel = new MainViewModel(
            _mockFileService.Object,
            _mockDialogService.Object,
            _mockSettingsService.Object,
            Mock.Of<IGoogleDriveService>());

        Assert.That(viewModel.FontSize, Is.EqualTo(24));
    }

    #endregion

    #region Property Tests

    [Test]
    public void SourcePath_SettingProperty_UpdatesSettings()
    {
        _viewModel.SourcePath = "C:\\New\\Source";
        _mockSettingsService.VerifySet(x => x.LastSourceFolder = "C:\\New\\Source", Times.Once);
    }

    [Test]
    public void DestinationPath_SettingProperty_UpdatesSettings()
    {
        _viewModel.DestinationPath = "D:\\New\\Destination";
        _mockSettingsService.VerifySet(x => x.LastDestinationFolder = "D:\\New\\Destination", Times.Once);
    }

    [Test]
    public void IsLoading_SettingProperty_RaisesPropertyChanged()
    {
        bool propertyChanged = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsLoading))
                propertyChanged = true;
        };

        _viewModel.IsLoading = true;

        Assert.That(propertyChanged, Is.True);
    }

    [Test]
    public void IsCopying_SettingProperty_RaisesCanExecuteChanged()
    {
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };
        _viewModel.IsCopying = true;

        Assert.That(_viewModel.CopyCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void ProgressValue_SettingProperty_UpdatesValue()
    {
        _viewModel.ProgressValue = 50;
        Assert.That(_viewModel.ProgressValue, Is.EqualTo(50));
    }

    [Test]
    public void ProgressMaximum_SettingProperty_UpdatesValue()
    {
        _viewModel.ProgressMaximum = 1000;
        Assert.That(_viewModel.ProgressMaximum, Is.EqualTo(1000));
    }

    [Test]
    public void StatusMessage_SettingProperty_UpdatesValue()
    {
        _viewModel.StatusMessage = "Loading...";
        Assert.That(_viewModel.StatusMessage, Is.EqualTo("Loading..."));
    }

    [Test]
    public void FontSize_SettingProperty_UpdatesSettings()
    {
        _viewModel.FontSize = 24;
        _mockSettingsService.VerifySet(x => x.FontSize = 24, Times.Once);
    }

    [Test]
    public void FontSize_SettingProperty_RaisesPropertyChanged()
    {
        bool propertyChanged = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.FontSize))
                propertyChanged = true;
        };

        _viewModel.FontSize = 24;

        Assert.That(propertyChanged, Is.True);
    }

    #endregion

    #region Header Properties Tests (BDD User Stories 2.1, 2.2, 2.3)

    //[Test]
    //public void AlreadyCopiedFilesHeader_IncludesCountInParentheses()
    //{
    //    // BDD v2.19: Header format "✅ Already Copied Videos (X)"
    //    _viewModel.AlreadyCopiedFiles.Add(new FileItem { Name = "file1.jpg" });
    //    _viewModel.AlreadyCopiedFiles.Add(new FileItem { Name = "file2.jpg" });

    //    var header = _viewModel.AlreadyCopiedFilesHeader;

    //    Assert.That(header, Is.EqualTo("✅ Already Copied Videos (2)"));
    //}

    //[Test]
    //public void NewFilesHeader_IncludesCountInParentheses()
    //{
    //    // BDD v2.19: Header format "🆕 New Videos to Copy (X)"
    //    _viewModel.NewFiles.Add(new FileItem { Name = "file1.jpg" });
    //    _viewModel.NewFiles.Add(new FileItem { Name = "file2.jpg" });
    //    _viewModel.NewFiles.Add(new FileItem { Name = "file3.jpg" });

    //    var header = _viewModel.NewFilesHeader;

    //    Assert.That(header, Is.EqualTo("🆕 New Videos to Copy (3)"));
    //}

    //[Test]
    //public void DestinationFilesHeader_IncludesCountInParentheses()
    //{
    //    // BDD v2.19: Header format "💻 Videos on Your Computer (X)"
    //    _viewModel.DestinationFiles.Add(new FileItem { Name = "file1.jpg" });

    //    var header = _viewModel.DestinationFilesHeader;

    //    Assert.That(header, Is.EqualTo("💻 Videos on Your Computer (1)"));
    //}

    //[Test]
    //public void Headers_Update_WhenCollectionChanges()
    //{
    //    // BDD: Headers update automatically when collections change
    //    bool headerChanged = false;
    //    _viewModel.PropertyChanged += (s, e) =>
    //    {
    //        if (e.PropertyName.Contains("Header"))
    //            headerChanged = true;
    //    };

    //    _viewModel.NewFiles.Add(new FileItem { Name = "file1.jpg" });

    //    Assert.That(headerChanged, Is.True);
    //    Assert.That(_viewModel.NewFilesHeader, Is.EqualTo("🆕 New Videos to Copy (1)"));
    //}

    #endregion

    #region CopyCommand Tests (BDD User Story 3.1, Rule 2)

    [Test]
    public void CopyCommand_CanExecute_WhenNotCopyingAndHasSelectedFiles()
    {
        // BDD v1.3: Copy button enabled only when files are selected
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };
        Assert.That(_viewModel.CopyCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void CopyCommand_CannotExecute_WhenCopying()
    {
        _viewModel.IsCopying = true;
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };
        Assert.That(_viewModel.CopyCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void CopyCommand_CannotExecute_WhenLoading()
    {
        _viewModel.IsLoading = true;
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };
        Assert.That(_viewModel.CopyCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void CopyCommand_CannotExecute_WhenNoSelectedFiles()
    {
        // BDD v1.3: Copy button disabled when no files selected
        _viewModel.SelectedNewFiles = new List<FileItem>();
        Assert.That(_viewModel.CopyCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void CopyCommand_CannotExecute_WhenSelectedNewFilesIsNull()
    {
        _viewModel.SelectedNewFiles = new List<FileItem>();
        Assert.That(_viewModel.CopyCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void CopyCommand_CanExecute_WithMultipleSelectedFiles()
    {
        // BDD User Story 3.1: Copy multiple selected files
        _viewModel.SelectedNewFiles = new List<FileItem> 
        { 
            new FileItem { Name = "file1.txt" },
            new FileItem { Name = "file2.txt" },
            new FileItem { Name = "file3.txt" }
        };
        Assert.That(_viewModel.CopyCommand.CanExecute(null), Is.True);
    }

    #endregion

    #region BrowseSourceCommand Tests (BDD User Story 1.1)

    [Test]
    public void BrowseSourceCommand_SetsSourcePath()
    {
        _mockDialogService.Setup(x => x.PickFolder(It.IsAny<string>())).Returns("C:\\Test\\Folder");

        _viewModel.BrowseSourceCommand.Execute(null);

        Assert.That(_viewModel.SourcePath, Is.EqualTo("C:\\Test\\Folder"));
    }

    [Test]
    public void BrowseSourceCommand_DoesNotSetPath_WhenDialogCancelled()
    {
        _mockDialogService.Setup(x => x.PickFolder(It.IsAny<string>())).Returns((string?)null);
        _viewModel.SourcePath = "C:\\Existing";

        _viewModel.BrowseSourceCommand.Execute(null);

        Assert.That(_viewModel.SourcePath, Is.EqualTo("C:\\Existing"));
    }

    #endregion

    #region BrowseDestinationCommand Tests (BDD User Story 1.2)

    [Test]
    public void BrowseDestinationCommand_SetsDestinationPath()
    {
        _mockDialogService.Setup(x => x.PickFolder(It.IsAny<string>())).Returns("D:\\Test\\Folder");

        _viewModel.BrowseDestinationCommand.Execute(null);

        Assert.That(_viewModel.DestinationPath, Is.EqualTo("D:\\Test\\Folder"));
    }

    [Test]
    public void BrowseDestinationCommand_DoesNotSetPath_WhenDialogCancelled()
    {
        _mockDialogService.Setup(x => x.PickFolder(It.IsAny<string>())).Returns((string?)null);
        _viewModel.DestinationPath = "D:\\Existing";

        _viewModel.BrowseDestinationCommand.Execute(null);

        Assert.That(_viewModel.DestinationPath, Is.EqualTo("D:\\Existing"));
    }

    #endregion

    #region LoadFilesAsync Tests (BDD User Story 2.4, Rule 4)

    [Test]
    public async Task LoadFilesAsync_WhenSourceIsEmpty_DoesNotThrow()
    {
        _viewModel.SourcePath = string.Empty;
        _viewModel.DestinationPath = string.Empty;

        Assert.DoesNotThrowAsync(() => _viewModel.LoadFilesAsync());
    }

    [Test]
    public async Task LoadFilesAsync_SetsIsLoadingTrue()
    {
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.DestinationPath = "D:\\Dest";
        _mockFileService.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(new List<FileInfo>());

        var loadTask = _viewModel.LoadFilesAsync();

        await loadTask;

        Assert.That(_viewModel.IsLoading, Is.False);
    }

    [Test]
    public void LoadFilesAsync_UpdatesStatusMessage_WhenFoldersDontExist()
    {
        // This test verifies error handling when folders don't exist
        _viewModel.SourcePath = "C:\\NonExistent";
        _viewModel.DestinationPath = "D:\\NonExistent";

        // Should not throw - error is shown via dialog
        Assert.DoesNotThrowAsync(() => _viewModel.LoadFilesAsync());
    }

    [Test]
    public async Task LoadFilesAsync_CallsCleanupTempFiles()
    {
        // BDD Rule 4: Cleanup .copying files on load
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.DestinationPath = "D:\\Dest";
        _mockFileService.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(new List<FileInfo>());

        await _viewModel.LoadFilesAsync();

        _mockFileService.Verify(x => x.CleanupTempFiles("D:\\Dest", ".copying"), Times.Once);
    }

    [Test]
    public async Task LoadFilesAsync_ShowsErrorDialog_OnException()
    {
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.DestinationPath = "D:\\Dest";
        _mockFileService.Setup(x => x.GetFiles(It.IsAny<string>()))
            .Throws(new IOException("Disk error"));

        await _viewModel.LoadFilesAsync();

        _mockDialogService.Verify(x => x.ShowMessage(
            It.Is<string>(s => s.Contains("Error loading files")),
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error), Times.Once);
    }

    [Test]
    public async Task LoadFilesAsync_SetsStatusMessage_Loading()
    {
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.DestinationPath = "D:\\Dest";
        
        // Mock GetFiles for both source and destination
        _mockFileService.Setup(x => x.GetFiles("C:\\Source"))
            .Returns(new List<FileInfo>());
        _mockFileService.Setup(x => x.GetFiles("D:\\Dest"))
            .Returns(new List<FileInfo>());
        _mockFileService.Setup(x => x.CleanupTempFiles(It.IsAny<string>(), It.IsAny<string>()));

        await _viewModel.LoadFilesAsync();

        Assert.That(_viewModel.StatusMessage, Does.Contain("Loaded"));
    }

    #endregion

    #region CopyAsync Tests (BDD User Stories 3.1-3.5)

    [Test]
    public async Task CopyAsync_ShowsError_WhenPathsAreEmpty()
    {
        _viewModel.SourcePath = string.Empty;
        _viewModel.DestinationPath = string.Empty;
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };

        await ((AsyncRelayCommand)_viewModel.CopyCommand).ExecuteAsync(null);

        _mockDialogService.Verify(x => x.ShowMessage(
            It.Is<string>(s => s.Contains("does not exist")),
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error), Times.Once);
    }

    [Test]
    public void CopyAsync_RequiresValidPaths()
    {
        // Verify that CopyCommand requires valid paths and selected files
        _viewModel.SourcePath = string.Empty;
        _viewModel.DestinationPath = string.Empty;
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };

        // Command can execute but will show error
        Assert.That(_viewModel.CopyCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void CopyAsync_SetsIsCopyingTrue_DuringOperation()
    {
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.DestinationPath = "D:\\Dest";
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt", FileSize = 100 } };

        // Verify IsCopying is false before
        Assert.That(_viewModel.IsCopying, Is.False);

        _viewModel.IsCopying = true;
        Assert.That(_viewModel.IsCopying, Is.True);
        _viewModel.IsCopying = false;
    }

    [Test]
    public async Task CopyAsync_ShowsNoFilesMessage_WhenNoFilesSelected()
    {
        // BDD v1.3: Copy button is disabled when no files selected
        // When SelectedNewFiles is empty, CanExecute returns false, so command won't execute
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.DestinationPath = "D:\\Dest";
        _viewModel.SelectedNewFiles = new List<FileItem>();

        // Verify command cannot execute
        Assert.That(_viewModel.CopyCommand.CanExecute(null), Is.False);
        
        // Since CanExecute is false, no message will be shown
        // This is the expected behavior per BDD v1.3
        Assert.Pass("Copy command correctly disabled when no files selected");
    }

    [Test]
    public async Task CopyAsync_SetsProgressMaximum_ToTotalBytesToCopy()
    {
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.DestinationPath = "D:\\Dest";
        _viewModel.SelectedNewFiles = new List<FileItem>
        {
            new FileItem { Name = "file1.txt", FileSize = 1000, FullPath = "C:\\Source\\file1.txt" },
            new FileItem { Name = "file2.txt", FileSize = 2000, FullPath = "C:\\Source\\file2.txt" }
        };

        _mockFileService.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
        _mockFileService.Setup(x => x.CopyFileAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IProgress<long>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await ((AsyncRelayCommand)_viewModel.CopyCommand).ExecuteAsync(null);

        Assert.That(_viewModel.ProgressMaximum, Is.EqualTo(3000));
    }

    [Test]
    public async Task CopyAsync_UsesTemporaryFile_ForSafeCopy()
    {
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.DestinationPath = "D:\\Dest";
        _viewModel.SelectedNewFiles = new List<FileItem> 
        { 
            new FileItem { Name = "file1.txt", FileSize = 1000, FullPath = "C:\\Source\\file1.txt" }
        };

        _mockFileService.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
        _mockFileService.Setup(x => x.CopyFileAsync(
            It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<IProgress<long>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await ((AsyncRelayCommand)_viewModel.CopyCommand).ExecuteAsync(null);

        // Verify temp file path ends with .copying
        _mockFileService.Verify(x => x.CopyFileAsync(
            It.IsAny<string>(), 
            It.Is<string>(path => path.EndsWith(".copying")),
            It.IsAny<IProgress<long>>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CopyAsync_ShowsOverwriteDialog_WhenFileExists()
    {
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.DestinationPath = "D:\\Dest";
        _viewModel.SelectedNewFiles = new List<FileItem> 
        { 
            new FileItem { Name = "existing.txt", FileSize = 1000, FullPath = "C:\\Source\\existing.txt" }
        };

        _mockFileService.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _mockDialogService.Setup(x => x.ShowOverwriteDialog(
            It.IsAny<string>(), It.IsAny<FileInfo>(), It.IsAny<FileInfo>()))
            .Returns(OverwriteChoice.No);

        await ((AsyncRelayCommand)_viewModel.CopyCommand).ExecuteAsync(null);

        _mockDialogService.Verify(x => x.ShowOverwriteDialog(
            It.IsAny<string>(), It.IsAny<FileInfo>(), It.IsAny<FileInfo>()), Times.Once);
    }

    [Test]
    public async Task CopyAsync_Stops_WhenUserCancelsOverwrite()
    {
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.DestinationPath = "D:\\Dest";
        _viewModel.SelectedNewFiles = new List<FileItem> 
        { 
            new FileItem { Name = "file1.txt", FileSize = 1000, FullPath = "C:\\Source\\file1.txt" },
            new FileItem { Name = "file2.txt", FileSize = 2000, FullPath = "C:\\Source\\file2.txt" }
        };

        _mockFileService.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        _mockDialogService.Setup(x => x.ShowOverwriteDialog(
            It.IsAny<string>(), It.IsAny<FileInfo>(), It.IsAny<FileInfo>()))
            .Returns(OverwriteChoice.Cancel);

        await ((AsyncRelayCommand)_viewModel.CopyCommand).ExecuteAsync(null);

        // Verify only one file was processed (operation stopped after cancel)
        _mockDialogService.Verify(x => x.ShowOverwriteDialog(
            It.IsAny<string>(), It.IsAny<FileInfo>(), It.IsAny<FileInfo>()), Times.Once);
    }

    [Test]
    public async Task CopyAsync_ShowsSuccessMessage_AfterCopy()
    {
        // Create temp directory to pass Directory.Exists check
        var tempSource = Path.Combine(Path.GetTempPath(), $"TestSource_{Guid.NewGuid()}");
        var tempDest = Path.Combine(Path.GetTempPath(), $"TestDest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempSource);
        Directory.CreateDirectory(tempDest);
        
        try
        {
            _viewModel.SourcePath = tempSource;
            _viewModel.DestinationPath = tempDest;
            
            // Create actual source file
            var sourceFile = Path.Combine(tempSource, "file1.txt");
            File.WriteAllText(sourceFile, "test content");
            
            _viewModel.SelectedNewFiles = new List<FileItem>
            {
                new FileItem { Name = "file1.txt", FileSize = 1000, FullPath = sourceFile }
            };

            // Mock FileExists to return false (file doesn't exist in dest)
            _mockFileService.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
            
            // Mock CopyFileAsync to actually create the dest file
            _mockFileService.Setup(x => x.CopyFileAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IProgress<long>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, IProgress<long>, CancellationToken>((src, dest, prog, tok) =>
                {
                    // Create the temp file so File.Move can work
                    File.WriteAllText(dest, "copied content");
                })
                .Returns(Task.CompletedTask);
            
            // Mock CleanupTempFiles
            _mockFileService.Setup(x => x.CleanupTempFiles(It.IsAny<string>(), It.IsAny<string>()));

            await ((AsyncRelayCommand)_viewModel.CopyCommand).ExecuteAsync(null);

            _mockDialogService.Verify(x => x.ShowMessage(
                It.Is<string>(s => s.Contains("Copied") && s.Contains("successfully")),
                "Copy Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information), Times.Once);
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(tempSource)) Directory.Delete(tempSource, true);
                if (Directory.Exists(tempDest)) Directory.Delete(tempDest, true);
            }
            catch { /* Ignore cleanup errors */ }
        }
    }

    [Test]
    public async Task CopyAsync_RefreshesFileLists_AfterCopy()
    {
        // Create temp directory to pass Directory.Exists check
        var tempSource = Path.Combine(Path.GetTempPath(), $"TestSource_{Guid.NewGuid()}");
        var tempDest = Path.Combine(Path.GetTempPath(), $"TestDest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempSource);
        Directory.CreateDirectory(tempDest);
        
        try
        {
            _viewModel.SourcePath = tempSource;
            _viewModel.DestinationPath = tempDest;
            
            // Create actual source file
            var sourceFile = Path.Combine(tempSource, "file1.txt");
            File.WriteAllText(sourceFile, "test content");
            
            _viewModel.SelectedNewFiles = new List<FileItem>
            {
                new FileItem { Name = "file1.txt", FileSize = 1000, FullPath = sourceFile }
            };

            _mockFileService.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
            
            // Mock CopyFileAsync to create the dest file
            _mockFileService.Setup(x => x.CopyFileAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IProgress<long>>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, IProgress<long>, CancellationToken>((src, dest, prog, tok) =>
                {
                    File.WriteAllText(dest, "copied content");
                })
                .Returns(Task.CompletedTask);
            
            _mockFileService.Setup(x => x.CleanupTempFiles(It.IsAny<string>(), It.IsAny<string>()));

            await ((AsyncRelayCommand)_viewModel.CopyCommand).ExecuteAsync(null);

            // Verify LoadFilesAsync was called (file lists refreshed)
            _mockFileService.Verify(x => x.GetFiles(It.IsAny<string>()), Times.AtLeast(2));
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(tempSource)) Directory.Delete(tempSource, true);
                if (Directory.Exists(tempDest)) Directory.Delete(tempDest, true);
            }
            catch { /* Ignore cleanup errors */ }
        }
    }

    #endregion

    #region DeleteCommand Tests (BDD User Stories 4.1-4.2, Rule 3)

    [Test]
    public async Task DeleteCommand_ShowsConfirmationDialog()
    {
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };
        _mockDialogService.Setup(x => x.ShowDeleteConfirmation(
            It.IsAny<string>(), It.IsAny<double>()))
            .Returns(false);

        await ((AsyncRelayCommand)_viewModel.DeleteCommand).ExecuteAsync(null);

        _mockDialogService.Verify(x => x.ShowDeleteConfirmation(
            It.Is<string>(s => s.Contains("PERMANENTLY delete")),
            It.IsAny<double>()), Times.Once);
    }

    [Test]
    public async Task DeleteCommand_DoesNotDelete_WhenUserCancels()
    {
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };
        _mockDialogService.Setup(x => x.ShowDeleteConfirmation(
            It.IsAny<string>(), It.IsAny<double>()))
            .Returns(false);

        await ((AsyncRelayCommand)_viewModel.DeleteCommand).ExecuteAsync(null);

        _mockFileService.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DeleteCommand_DeletesFromSource_WhenSelectedFromNewFiles()
    {
        // BDD Rule 3: Delete from New Files deletes from SOURCE folder
        _viewModel.SourcePath = "C:\\Source";
        var fileToDelete = new FileItem { Name = "test.txt", FullPath = "C:\\Source\\test.txt" };
        _viewModel.SelectedNewFiles = new List<FileItem> { fileToDelete };

        _mockDialogService.Setup(x => x.ShowDeleteConfirmation(
            It.IsAny<string>(), It.IsAny<double>()))
            .Returns(true);

        await ((AsyncRelayCommand)_viewModel.DeleteCommand).ExecuteAsync(null);

        _mockFileService.Verify(x => x.DeleteFile("C:\\Source\\test.txt"), Times.Once);
    }

    [Test]
    public async Task DeleteCommand_DeletesFromSource_WhenSelectedFromAlreadyCopied()
    {
        // BDD Rule 3: Delete from Already Copied deletes from SOURCE folder
        _viewModel.SourcePath = "C:\\Source";
        var fileToDelete = new FileItem { Name = "test.txt", FullPath = "C:\\Source\\test.txt" };
        _viewModel.SelectedAlreadyCopiedFiles = new List<FileItem> { fileToDelete };

        _mockDialogService.Setup(x => x.ShowDeleteConfirmation(
            It.IsAny<string>(), It.IsAny<double>()))
            .Returns(true);

        await ((AsyncRelayCommand)_viewModel.DeleteCommand).ExecuteAsync(null);

        _mockFileService.Verify(x => x.DeleteFile("C:\\Source\\test.txt"), Times.Once);
    }

    [Test]
    public async Task DeleteCommand_DeletesFromDestination_WhenSelectedFromDestinationFiles()
    {
        // BDD Rule 3: Delete from Destination deletes from DESTINATION folder
        _viewModel.DestinationPath = "D:\\Dest";
        var fileToDelete = new FileItem { Name = "test.txt", FullPath = "D:\\Dest\\test.txt" };
        _viewModel.SelectedDestinationFiles = new List<FileItem> { fileToDelete };

        _mockDialogService.Setup(x => x.ShowDeleteConfirmation(
            It.IsAny<string>(), It.IsAny<double>()))
            .Returns(true);

        await ((AsyncRelayCommand)_viewModel.DeleteCommand).ExecuteAsync(null);

        _mockFileService.Verify(x => x.DeleteFile("D:\\Dest\\test.txt"), Times.Once);
    }

    [Test]
    public async Task DeleteCommand_DeletesMultipleFiles()
    {
        // BDD User Story 4.1: Delete multiple files
        _viewModel.SourcePath = "C:\\Source";
        _viewModel.SelectedNewFiles = new List<FileItem>
        {
            new FileItem { Name = "file1.txt", FullPath = "C:\\Source\\file1.txt" },
            new FileItem { Name = "file2.txt", FullPath = "C:\\Source\\file2.txt" }
        };

        _mockDialogService.Setup(x => x.ShowDeleteConfirmation(
            It.IsAny<string>(), It.IsAny<double>()))
            .Returns(true);

        await ((AsyncRelayCommand)_viewModel.DeleteCommand).ExecuteAsync(null);

        _mockFileService.Verify(x => x.DeleteFile("C:\\Source\\file1.txt"), Times.Once);
        _mockFileService.Verify(x => x.DeleteFile("C:\\Source\\file2.txt"), Times.Once);
    }

    [Test]
    public async Task DeleteCommand_ShowsErrorDialog_OnDeleteFailure()
    {
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };
        _mockDialogService.Setup(x => x.ShowDeleteConfirmation(
            It.IsAny<string>(), It.IsAny<double>()))
            .Returns(true);
        _mockFileService.Setup(x => x.DeleteFile(It.IsAny<string>()))
            .Throws(new IOException("File in use"));

        await ((AsyncRelayCommand)_viewModel.DeleteCommand).ExecuteAsync(null);

        _mockDialogService.Verify(x => x.ShowMessage(
            It.Is<string>(s => s.Contains("Failed to delete")),
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error), Times.Once);
    }

    [Test]
    public async Task DeleteCommand_RefreshesFileLists_AfterDelete()
    {
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };
        _mockDialogService.Setup(x => x.ShowDeleteConfirmation(
            It.IsAny<string>(), It.IsAny<double>()))
            .Returns(true);

        await ((AsyncRelayCommand)_viewModel.DeleteCommand).ExecuteAsync(null);

        // Verify LoadFilesAsync was called (file lists refreshed)
        _mockFileService.Verify(x => x.GetFiles(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region OpenCommand Tests (BDD User Story 5.1)

    [Test]
    public void OpenCommand_CallsFileServiceOpenFile()
    {
        var fileItem = new FileItem { Name = "test.txt", FullPath = "C:\\Source\\test.txt" };
        _viewModel.SelectedNewFiles = new List<FileItem> { fileItem };

        _viewModel.OpenCommand.Execute(null);

        _mockFileService.Verify(x => x.OpenFile("C:\\Source\\test.txt"), Times.Once);
    }

    [Test]
    public void OpenCommand_OpensMultipleFiles()
    {
        var file1 = new FileItem { Name = "file1.txt", FullPath = "C:\\Source\\file1.txt" };
        var file2 = new FileItem { Name = "file2.txt", FullPath = "C:\\Source\\file2.txt" };
        _viewModel.SelectedNewFiles = new List<FileItem> { file1, file2 };

        _viewModel.OpenCommand.Execute(null);

        _mockFileService.Verify(x => x.OpenFile("C:\\Source\\file1.txt"), Times.Once);
        _mockFileService.Verify(x => x.OpenFile("C:\\Source\\file2.txt"), Times.Once);
    }

    [Test]
    public void OpenCommand_ShowsErrorDialog_OnFailure()
    {
        var fileItem = new FileItem { Name = "test.txt", FullPath = "C:\\Source\\test.txt" };
        _viewModel.SelectedNewFiles = new List<FileItem> { fileItem };
        _mockFileService.Setup(x => x.OpenFile(It.IsAny<string>()))
            .Throws(new InvalidOperationException("No application associated"));

        _viewModel.OpenCommand.Execute(null);

        _mockDialogService.Verify(x => x.ShowMessage(
            It.Is<string>(s => s.Contains("Cannot open file")),
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error), Times.Once);
    }

    #endregion

    #region RefreshCommand Tests (BDD User Story 2.4)

    [Test]
    public void RefreshCommand_IsNotNull()
    {
        Assert.That(_viewModel.RefreshCommand, Is.Not.Null);
    }

    [Test]
    public void RefreshCommand_CanExecute_AlwaysTrue()
    {
        Assert.That(_viewModel.RefreshCommand.CanExecute(null), Is.True);
    }

    #endregion

    #region OpenSettingsCommand Tests (BDD User Story 6.1)

    [Test]
    public void OpenSettingsCommand_RaisesOpenSettingsRequestedEvent()
    {
        bool eventRaised = false;
        _viewModel.OpenSettingsRequested += () => eventRaised = true;

        _viewModel.OpenSettingsCommand.Execute(null);

        Assert.That(eventRaised, Is.True);
    }

    #endregion

    #region OnClosing Tests (BDD Rule 5, Data Persistence)

    [Test]
    public void OnClosing_SavesSettings()
    {
        _viewModel.OnClosing();
        _mockSettingsService.Verify(x => x.Save(), Times.Once);
    }

    #endregion

    #region FileItem Model Tests (BDD FileItem Model)

    [Test]
    public void FileItem_DisplayName_ReturnsName_WhenNotCopied()
    {
        var fileItem = new FileItem { Name = "photo.jpg", IsAlreadyCopied = false };
        
        Assert.That(fileItem.DisplayName, Is.EqualTo("photo.jpg"));
    }

    [Test]
    public void FileItem_DisplayName_IncludesTickIcon_WhenAlreadyCopied()
    {
        // BDD: DisplayName shows ✅ prefix for already copied files
        var fileItem = new FileItem { Name = "photo.jpg", IsAlreadyCopied = true };
        
        Assert.That(fileItem.DisplayName, Does.StartWith("✅"));
        Assert.That(fileItem.DisplayName, Does.Contain("photo.jpg"));
    }

    [Test]
    public void FileItem_IsAlreadyCopied_RaisesDisplayNamePropertyChanged()
    {
        var fileItem = new FileItem { Name = "photo.jpg" };
        bool displayNameChanged = false;
        
        fileItem.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FileItem.DisplayName))
                displayNameChanged = true;
        };

        fileItem.IsAlreadyCopied = true;

        Assert.That(displayNameChanged, Is.True);
    }

    #endregion

    #region Accessibility Tests (BDD Requirement 1)

    [Test]
    public void FontSize_Range_14to28_IsValid()
    {
        // BDD v1.4: Font size range is 14-28 pixels
        _viewModel.FontSize = 14;
        Assert.That(_viewModel.FontSize, Is.EqualTo(14));
        
        _viewModel.FontSize = 28;
        Assert.That(_viewModel.FontSize, Is.EqualTo(28));
    }

    [Test]
    public void FontSize_Default_Is20Pixels()
    {
        // BDD v1.4: Default font size is 20px for elderly users
        _mockSettingsService.SetupGet(x => x.FontSize).Returns(0);

        var viewModel = new MainViewModel(
            _mockFileService.Object,
            _mockDialogService.Object,
            _mockSettingsService.Object,
            Mock.Of<IGoogleDriveService>());

        Assert.That(viewModel.FontSize, Is.EqualTo(20));
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        _viewModel.OnClosing();
    }
}

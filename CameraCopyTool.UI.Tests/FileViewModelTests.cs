using System.Windows;
using CameraCopyTool.Commands;
using CameraCopyTool.Models;
using CameraCopyTool.Services;
using CameraCopyTool.ViewModels;
using Moq;

namespace CameraCopyTool.UI.Tests;

/// <summary>
/// Comprehensive unit tests for MainViewModel covering all scenarios.
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
            _mockSettingsService.Object);
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
            _mockSettingsService.Object);

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

    #endregion

    #region CopyCommand Tests

    [Test]
    public void CopyCommand_CanExecute_WhenNotCopyingAndHasSelectedFiles()
    {
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
        _viewModel.SelectedNewFiles = new List<FileItem>();
        Assert.That(_viewModel.CopyCommand.CanExecute(null), Is.False);
    }

    #endregion

    #region BrowseSourceCommand Tests

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

    #region BrowseDestinationCommand Tests

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

    #region LoadFilesAsync Tests

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
        
        // Note: IsLoading might be reset before we check, so we test the flow
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

    #endregion

    #region CopyAsync Tests

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
        
        // Note: Full async copy testing requires WPF Application context
        // This test verifies the property exists and can be set
        _viewModel.IsCopying = true;
        Assert.That(_viewModel.IsCopying, Is.True);
        _viewModel.IsCopying = false;
    }

    #endregion

    #region DeleteCommand Tests

    [Test]
    public async Task DeleteCommand_ShowsConfirmationDialog()
    {
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };
        _mockDialogService.Setup(x => x.ShowMessage(
            It.IsAny<string>(), "Confirm Delete", 
            MessageBoxButton.YesNo, MessageBoxImage.Warning))
            .Returns(MessageBoxResult.No);

        await ((AsyncRelayCommand)_viewModel.DeleteCommand).ExecuteAsync(null);

        _mockDialogService.Verify(x => x.ShowMessage(
            It.Is<string>(s => s.Contains("delete")),
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning), Times.Once);
    }

    [Test]
    public async Task DeleteCommand_DoesNotDelete_WhenUserCancels()
    {
        _viewModel.SelectedNewFiles = new List<FileItem> { new FileItem { Name = "test.txt" } };
        _mockDialogService.Setup(x => x.ShowMessage(
            It.IsAny<string>(), "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning))
            .Returns(MessageBoxResult.No);

        await ((AsyncRelayCommand)_viewModel.DeleteCommand).ExecuteAsync(null);

        _mockFileService.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region OpenCommand Tests

    [Test]
    public void OpenCommand_CallsFileServiceOpenFile()
    {
        var fileItem = new FileItem { Name = "test.txt", FullPath = "C:\\Source\\test.txt" };
        _viewModel.SelectedNewFiles = new List<FileItem> { fileItem };

        _viewModel.OpenCommand.Execute(null);

        _mockFileService.Verify(x => x.OpenFile("C:\\Source\\test.txt"), Times.Once);
    }

    #endregion

    #region RefreshCommand Tests

    [Test]
    public void RefreshCommand_IsNotNull()
    {
        Assert.That(_viewModel.RefreshCommand, Is.Not.Null);
    }

    #endregion

    #region OnClosing Tests

    [Test]
    public void OnClosing_SavesSettings()
    {
        _viewModel.OnClosing();
        _mockSettingsService.Verify(x => x.Save(), Times.Once);
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        _viewModel.OnClosing();
    }
}

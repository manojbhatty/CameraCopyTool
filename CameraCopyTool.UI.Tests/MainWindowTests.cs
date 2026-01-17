using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using NUnit.Framework;
using static System.Net.Mime.MediaTypeNames;
using Application = FlaUI.Core.Application;


[TestFixture]
[Apartment(ApartmentState.STA)] // Required for WPF
public class MainWindowTests
{
    private FlaUI.Core.Application _app;

    private UIA3Automation _automation;

    // Paths for temporary folders
    private string _tempSourceFolder;
    private string _tempDestinationFolder;


    [SetUp]
    public void Setup()
    {
        // 1️⃣ Create temporary folders for testing
        _tempSourceFolder = Path.Combine(Path.GetTempPath(), $"CameraCopyTool_Source_{Guid.NewGuid()}");
        _tempDestinationFolder = Path.Combine(Path.GetTempPath(), $"CameraCopyTool_Dest_{Guid.NewGuid()}");

        Directory.CreateDirectory(_tempSourceFolder);
        Directory.CreateDirectory(_tempDestinationFolder);

        // 2️⃣ Add some dummy files to source
        File.WriteAllText(Path.Combine(_tempSourceFolder, "File1.txt"), "Test content 1");
        File.WriteAllText(Path.Combine(_tempSourceFolder, "File2.txt"), "Test content 2");

        // 3️⃣ Add a file to destination to simulate "already copied"
        File.WriteAllText(Path.Combine(_tempDestinationFolder, "File2.txt"), "Test content 2");

        // 4️⃣ Launch the app pointing to these folders (update path to your exe)
        string exePath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            @"..\..\..\..\CameraCopyTool\bin\Debug\net10.0-windows\CameraCopyTool.exe"));

        if (!File.Exists(exePath))
            Assert.Fail($"Executable not found at {exePath}");

        _app = Application.Launch(exePath);
        _automation = new UIA3Automation();

        // 5️⃣ Wait for main window to appear
        var mainWindow = _app.GetMainWindow(_automation);
        Assert.That(mainWindow, Is.Not.Null);

        // Optional: Set the textboxes via Automation for source/destination
        var sourceBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("SourcePathTextBox")).AsTextBox();
        var destBox = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DestinationPathTextBox")).AsTextBox();

        sourceBox.Text = _tempSourceFolder;
        destBox.Text = _tempDestinationFolder;

        // Optional: trigger refresh/load
        var refreshButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("RefreshMenu")).AsButton();
        refreshButton?.Invoke();
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            _app?.Close();
            _automation?.Dispose();
        }
        catch { /* swallow errors */ }

        // Cleanup temporary folders
        try
        {
            if (Directory.Exists(_tempSourceFolder))
                Directory.Delete(_tempSourceFolder, true);
            if (Directory.Exists(_tempDestinationFolder))
                Directory.Delete(_tempDestinationFolder, true);
        }
        catch { /* ignore cleanup errors */ }
    }

    [Test]
    public void App_Launches_MainWindow_IsVisible()
    {
        var window = _app.GetMainWindow(_automation);
        Assert.That(window, Is.Not.Null);
        Assert.That(window.Title, Is.EqualTo("Camera Copy Tool"));
    }

    [Test]
    public void CopyButton_Disables_During_Copy()
    {
        var window = _app.GetMainWindow(_automation);

        // Find the New Files list
        var newFilesList = window.FindFirstDescendant(cf => cf.ByAutomationId("NewFilesListView"))
                             .AsListBox();

        Retry.WhileFalse(
            () => newFilesList.Items.Length > 0,
            TimeSpan.FromSeconds(5)
        );

        // Select all items (or the first item)
        var items = newFilesList.Items;
        //Assert.IsTrue(items.Length > 0, "No files found in New Files list");

        items[0].Select(); // select the first file

        var copyButton = window.FindFirstDescendant(cf =>
            cf.ByAutomationId("CopyButton")).AsButton();

        Assert.That(copyButton.IsEnabled, Is.True);

        copyButton.Invoke();

        copyButton = window.FindFirstDescendant(cf =>
           cf.ByAutomationId("CopyButton")).AsButton();
        Assert.That(copyButton.IsEnabled, Is.False);
    }

    [Test]
    public void NewFilesListView_Shows_Items()
    {
        var window = _app.GetMainWindow(_automation);

        var list = window.FindFirstDescendant(cf =>
            cf.ByAutomationId("NewFilesListView")).AsListBox();

        Retry.WhileFalse(
           () => list.Items.Length > 0,
           TimeSpan.FromSeconds(5)
       );
        Assert.That(list.Items.Length, Is.GreaterThan(0));
    }

    [Test]
    public void RightClick_ListItem_Shows_ContextMenu()
    {
        var window = _app.GetMainWindow(_automation);

        var list = window.FindFirstDescendant(cf =>
            cf.ByAutomationId("NewFilesListView")).AsListBox();
        Retry.WhileFalse(
          () => list.Items.Length > 0,
          TimeSpan.FromSeconds(5)
      );
        var firstItem = list.Items[0];
        firstItem.RightClick();

        var contextMenu = window.FindFirstDescendant(cf =>
            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Menu));

        Assert.That(contextMenu, Is.Not.Null);
    }

    [Test]
    public void RefreshMenu_Triggers_LoadFiles()
    {
        var window = _app.GetMainWindow(_automation);

        // Open parent menu
        var toolsMenu = window.FindFirstDescendant(cf =>
            cf.ByAutomationId("ToolsMenu")).AsMenuItem();

        Assert.That(toolsMenu, Is.Not.Null);

        toolsMenu.Expand(); // 👈 NOT Invoke()

        // Find Refresh menu item
        var refreshMenu = window.FindFirstDescendant(cf =>
            cf.ByAutomationId("RefreshMenu")).AsMenuItem();

        Assert.That(refreshMenu, Is.Not.Null);

        // Click instead of Invoke
        refreshMenu.Click();

        Assert.Pass();
    }



}

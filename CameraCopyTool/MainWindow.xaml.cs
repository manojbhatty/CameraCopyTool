using Microsoft.WindowsAPICodePack.Dialogs; // For modern folder picker
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Security.Cryptography;

namespace CameraCopyTool
{
    public partial class MainWindow : Window
    {
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += Window_Closing;
        }

        #region Startup and Settings

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastSourceFolder))
                txtSourcePath.Text = Properties.Settings.Default.LastSourceFolder;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastDestinationFolder))
                txtDestinationPath.Text = Properties.Settings.Default.LastDestinationFolder;

            // Ensure XAML is fully loaded before accessing overlay
            await Dispatcher.InvokeAsync(async () =>
            {
                await LoadFilesAsync();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save last-used folders
            Properties.Settings.Default.LastSourceFolder = txtSourcePath.Text;
            Properties.Settings.Default.LastDestinationFolder = txtDestinationPath.Text;
            Properties.Settings.Default.Save();
        }

        #endregion

        #region Browse Folder

        private void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            string path = PickFolder(txtSourcePath.Text);
            if (!string.IsNullOrEmpty(path))
                txtSourcePath.Text = path;
        }

        private void BrowseDestination_Click(object sender, RoutedEventArgs e)
        {
            string path = PickFolder(txtDestinationPath.Text);
            if (!string.IsNullOrEmpty(path))
                txtDestinationPath.Text = path;
        }

        private string PickFolder(string initialPath)
        {
            var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
            if (Directory.Exists(initialPath))
                dlg.InitialDirectory = initialPath;

            return dlg.ShowDialog() == CommonFileDialogResult.Ok ? dlg.FileName : null;
        }

        #endregion

        #region TextBox Changes
        private CancellationTokenSource _cts;
        private async void SourcePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lvAlreadyCopied == null) return;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await Task.Delay(300, token); // 300ms debounce
                if (!token.IsCancellationRequested)
                    await LoadFilesAsync();
            }
            catch (TaskCanceledException) { }

        }

        private async void DestinationPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lvAlreadyCopied == null) return;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await Task.Delay(300, token); // 300ms debounce
                if (!token.IsCancellationRequested)
                    await LoadFilesAsync();
            }
            catch (TaskCanceledException) { }

        }

        #endregion

        #region Load Files

        private async Task LoadFilesAsync()
        {
            if (LoadingOverlay != null)
                LoadingOverlay.Visibility = Visibility.Visible;

            // ✅ Capture paths on UI thread
            string sourcePath = txtSourcePath.Text;
            string destPath = txtDestinationPath.Text;

            List<FileItem> alreadyCopied = new();
            List<FileItem> newFiles = new();
            List<FileItem> destFileItems = new();

            await Task.Run(() =>
            {
                // ✅ Only use the local strings here, no UI access
                var sourceFiles = Directory.Exists(sourcePath)
                    ? Directory.GetFiles(sourcePath).Select(f => new FileInfo(f)).ToList()
                    : new List<FileInfo>();

                var destFiles = Directory.Exists(destPath)
                    ? Directory.GetFiles(destPath).Select(f => new FileInfo(f)).ToList()
                    : new List<FileInfo>();

                var sourceNames = sourceFiles.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var f in destFiles)
                {
                    destFileItems.Add(new FileItem
                    {
                        Name = f.Name,
                        ModifiedDate = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        IsAlreadyCopied = sourceNames.Contains(f.Name)
                    });
                }

                foreach (var src in sourceFiles)
                {
                    bool existsInDest = destFiles.Any(d => d.Name == src.Name && d.Length == src.Length);
                    var fileItem = new FileItem
                    {
                        Name = src.Name,
                        ModifiedDate = src.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        IsAlreadyCopied = existsInDest
                    };

                    if (existsInDest)
                        alreadyCopied.Add(fileItem);
                    else
                        newFiles.Add(fileItem);
                }
            });

            // ✅ Update UI safely on the main thread
            lvAlreadyCopied.Items.Clear();
            lvNewFiles.Items.Clear();
            lvDestinationFiles.Items.Clear();

            foreach (var f in alreadyCopied)
                lvAlreadyCopied.Items.Add(f);

            foreach (var f in newFiles)
                lvNewFiles.Items.Add(f);

            foreach (var f in destFileItems)
                lvDestinationFiles.Items.Add(f);

            if (LoadingOverlay != null)
                LoadingOverlay.Visibility = Visibility.Collapsed;

            // Update GroupBox headers with counts
            gbAlreadyCopied.Header = $"Already copied files - {lvAlreadyCopied.Items.Count}";
            gbNewFiles.Header = $"New files - {lvNewFiles.Items.Count}";
            gbDestinationFiles.Header = $"Files in destination - {lvDestinationFiles.Items.Count}";
        }




        private void LoadFiles()
        {
            // Check that both folders exist
            if (!Directory.Exists(txtSourcePath.Text) || !Directory.Exists(txtDestinationPath.Text))
                return;

            // Clear all ListViews
            lvAlreadyCopied.Items.Clear();
            lvNewFiles.Items.Clear();
            lvDestinationFiles.Items.Clear();

            // Get files
            var sourceFiles = Directory.GetFiles(txtSourcePath.Text)
                                       .Select(f => new FileInfo(f))
                                       .ToList();

            var destFiles = Directory.GetFiles(txtDestinationPath.Text)
                                     .Select(f => new FileInfo(f))
                                     .ToList();

            // Build a HashSet of source file names for quick lookup
            var sourceFileNames = sourceFiles.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Populate destination ListView with ticks for already copied files
            foreach (var f in destFiles)
            {
                lvDestinationFiles.Items.Add(new FileItem
                {
                    Name = f.Name,
                    ModifiedDate = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                    IsAlreadyCopied = sourceFileNames.Contains(f.Name)
                });
            }

            // Populate AlreadyCopied and NewFiles ListViews from source
            foreach (var src in sourceFiles)
            {
                bool existsInDest = destFiles.Any(d => d.Name == src.Name && d.Length == src.Length);

                var fileItem = new FileItem
                {
                    Name = src.Name,
                    ModifiedDate = src.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                    IsAlreadyCopied = existsInDest // optional, used only if you want to style AlreadyCopied panel
                };

                if (existsInDest)
                    lvAlreadyCopied.Items.Add(fileItem);
                else
                    lvNewFiles.Items.Add(fileItem);
            }
        }



        #endregion

        #region Copy Button (Chunked with Progress Bar)

        private async void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            string sourceFolder = txtSourcePath.Text;
            string destFolder = txtDestinationPath.Text;

            if (!Directory.Exists(sourceFolder) || !Directory.Exists(destFolder))
            {
                MessageBox.Show("Source or destination folder does not exist!", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var filesToCopy = lvNewFiles.SelectedItems.Cast<FileItem>().ToList();

            if (filesToCopy.Count == 0)
            {
                MessageBox.Show("No files selected to copy!", "Info",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ✅ Disable copy button during operation
            btnCopy.IsEnabled = false;
            btnCopy.Content = "Copying...";
            pbCopyProgress.Minimum = 0;
            pbCopyProgress.Maximum = filesToCopy.Sum(f => new FileInfo(Path.Combine(sourceFolder, f.Name)).Length);
            pbCopyProgress.Value = 0;
            try
            {
                int successCount = 0;
                var copiedFiles = new System.Collections.Generic.List<FileItem>();

                await System.Threading.Tasks.Task.Run(() =>
                {
                    foreach (var fileItem in filesToCopy)
                    {
                        string sourcePath = Path.Combine(sourceFolder, fileItem.Name);
                        string destPath = Path.Combine(destFolder, fileItem.Name);

                        try
                        {
                            // Check if file exists in destination
                            if (File.Exists(destPath))
                            {
                                FileInfo srcInfo = new FileInfo(sourcePath);
                                FileInfo destInfo = new FileInfo(destPath);

                                string msg = $"File '{fileItem.Name}' already exists in destination.\n" +
                                             $"Source: {srcInfo.Length / 1024} KB, Last modified: {srcInfo.LastWriteTime}\n" +
                                             $"Destination: {destInfo.Length / 1024} KB, Last modified: {destInfo.LastWriteTime}\n\n" +
                                             "Do you want to overwrite it?";

                                // Ask user on the UI thread
                                bool overwrite = false;
                                Dispatcher.Invoke(() =>
                                {
                                    var result = MessageBox.Show(msg, "Overwrite file?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                                    overwrite = result == MessageBoxResult.Yes;

                                    // Cancel copy operation entirely
                                    if (result == MessageBoxResult.Cancel)
                                    {
                                        throw new OperationCanceledException("Copy cancelled by user.");
                                    }
                                });

                                if (!overwrite)
                                    continue; // Skip this file
                            }

                            // Perform chunked copy
                            using (var sourceStream = File.OpenRead(sourcePath))
                            using (var destStream = File.OpenWrite(destPath))
                            {
                                byte[] buffer = new byte[81920]; // 80 KB buffer
                                int bytesRead;
                                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    destStream.Write(buffer, 0, bytesRead);
                                    Dispatcher.Invoke(() =>
                                    {
                                        pbCopyProgress.Value += bytesRead;
                                    });
                                }
                            }

                            copiedFiles.Add(fileItem);
                            successCount++;
                        }
                        catch (OperationCanceledException)
                        {
                            // Stop entire copy
                            break;
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"Failed to copy {fileItem.Name}:\n{ex.Message}", "Error",
                                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            });
                        }
                    }
                });

                // Move copied files in ListViews
                foreach (var fileItem in copiedFiles)
                {
                    lvNewFiles.Items.Remove(fileItem);
                    lvAlreadyCopied.Items.Add(fileItem);
                }

                // Reset progress
                pbCopyProgress.Value = 0;

                if (successCount > 0)
                    MessageBox.Show($"Copied {successCount} file(s) successfully!", "Copy Complete",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadFilesAsync();
            }
            finally
            {
                // ✅ Re-enable the button no matter what
                btnCopy.IsEnabled = true;
                pbCopyProgress.Value = 0;
                btnCopy.Content = "Copy ➜";
            }

        }


        #endregion

        #region Context Menu: Open / Delete

        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menu)
                return;

            if (menu.Parent is not ContextMenu contextMenu)
                return;

            if (contextMenu.PlacementTarget is not ListViewItem listViewItem)
                return;

            var listView = FindParent<ListView>(listViewItem);
            if (listView == null)
                return;

            foreach (var item in listView.SelectedItems.Cast<FileItem>())
            {
                string folderPath = listView == lvNewFiles
                    ? txtSourcePath.Text
                    : txtDestinationPath.Text;

                string filePath = Path.Combine(folderPath, item.Name);

                if (!File.Exists(filePath))
                    continue;

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Cannot open file {item.Name}:\n{ex.Message}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private async void Menu_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menu)
                return;

            if (menu.Parent is not ContextMenu contextMenu)
                return;

            if (contextMenu.PlacementTarget is not ListViewItem listViewItem)
                return;

            var listView = FindParent<ListView>(listViewItem);
            if (listView == null)
                return;

            var itemsToDelete = listView.SelectedItems.Cast<FileItem>().ToList();
            if (itemsToDelete.Count == 0)
                return;

            if (MessageBox.Show(
                    $"Are you sure you want to delete {itemsToDelete.Count} file(s)?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            // 🔑 Determine correct folder based on ListView
            string folderPath;
            if (listView == lvDestinationFiles)
                folderPath = txtDestinationPath.Text;
            else if (listView == lvNewFiles || listView == lvAlreadyCopied)
                folderPath = txtSourcePath.Text;
            else
                return;

            foreach (var fileItem in itemsToDelete)
            {
                string filePath = Path.Combine(folderPath, fileItem.Name);

                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    listView.Items.Remove(fileItem);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete {fileItem.Name}:\n{ex.Message}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            await LoadFilesAsync();

        }



        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader header || header.Column == null)
                return;

            // Property to sort by from Tag
            string sortBy = header.Tag as string;
            if (string.IsNullOrEmpty(sortBy))
                return;

            // Find parent ListView
            ListView listView = FindParent<ListView>(header);
            if (listView == null)
                return;

            // Determine sort direction
            ListSortDirection direction;
            if (_lastHeaderClicked == header)
                direction = _lastDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            else
                direction = ListSortDirection.Ascending;

            // Clear arrows from all columns in this listview
            ClearSortArrows(listView);

            // Apply sort
            SortListView(listView, sortBy, direction);

            // Set arrow on clicked header
            SetSortArrow(header, direction);

            _lastHeaderClicked = header;
            _lastDirection = direction;
        }



        private void SortListView(ListView listView, string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(listView.ItemsSource ?? listView.Items);

            if (dataView != null)
            {
                dataView.SortDescriptions.Clear();
                dataView.SortDescriptions.Add(new SortDescription(sortBy, direction));
                dataView.Refresh();
            }
        }

        private void HighlightAlreadyCopiedInDestination()
        {
            lvDestinationFiles.SelectedItems.Clear();

            // Build a set of already copied file names for fast lookup
            var copiedNames = lvAlreadyCopied.Items.Cast<FileItem>()
                                                   .Select(f => f.Name)
                                                   .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in lvDestinationFiles.Items.Cast<FileItem>())
            {
                if (copiedNames.Contains(item.Name))
                    lvDestinationFiles.SelectedItems.Add(item);
            }

            // Scroll into view the last selected item
            if (lvDestinationFiles.SelectedItems.Count > 0)
                lvDestinationFiles.ScrollIntoView(lvDestinationFiles.SelectedItems[lvDestinationFiles.SelectedItems.Count - 1]);
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = child;

            while (parent != null)
            {
                if (parent is T correctlyTyped)
                    return correctlyTyped;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        private void ClearSortArrows(ListView listView)
        {
            if (listView.View is not GridView gridView)
                return;

            foreach (var column in gridView.Columns)
            {
                if (column.Header is GridViewColumnHeader header)
                {
                    header.Content = header.Content.ToString().Replace(" ▲", "").Replace(" ▼", "");
                }
            }
        }


        private void SetSortArrow(GridViewColumnHeader header, ListSortDirection direction)
        {
            string baseText = header.Content.ToString()
                                            .Replace(" ▲", "")
                                            .Replace(" ▼", "");

            header.Content = direction == ListSortDirection.Ascending
                ? $"{baseText} ▲"
                : $"{baseText} ▼";
        }

        private void ListViewItem_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item)
            {
                item.IsSelected = true;
                item.Focus();
            }
        }
        #endregion

        private async void RefreshMenu_Click(object sender, RoutedEventArgs e)
        {
            // Optional: disable the menu item while refreshing
            if (sender is MenuItem menuItem)
                menuItem.IsEnabled = false;

            try
            {
                await LoadFilesAsync();
            }
            finally
            {
                if (sender is MenuItem menuItem2)
                    menuItem2.IsEnabled = true;
            }
        }

        private static string ComputeFileHash(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(stream);
            return Convert.ToHexString(hash); // .NET 5+
        }
    }
}

using Microsoft.WindowsAPICodePack.Dialogs; // For modern folder picker
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel; // For ListSortDirection
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // For GridViewColumnHeader
using System.Windows.Data;
using System.Windows.Media;

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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load last-used folders from settings
            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastSourceFolder))
                txtSourcePath.Text = Properties.Settings.Default.LastSourceFolder;

            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastDestinationFolder))
                txtDestinationPath.Text = Properties.Settings.Default.LastDestinationFolder;

            LoadFiles();
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

        private void SourcePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lvAlreadyCopied == null) return;
            LoadFiles();
        }

        private void DestinationPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lvAlreadyCopied == null) return;
            LoadFiles();
        }

        #endregion

        #region Load Files

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

            pbCopyProgress.Minimum = 0;
            pbCopyProgress.Maximum = filesToCopy.Sum(f => new FileInfo(Path.Combine(sourceFolder, f.Name)).Length);
            pbCopyProgress.Value = 0;

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

            // Highlight newly copied files
            lvAlreadyCopied.SelectedItems.Clear();
            foreach (var fileItem in copiedFiles)
                lvAlreadyCopied.SelectedItems.Add(fileItem);

            if (lvAlreadyCopied.Items.Count > 0)
                lvAlreadyCopied.ScrollIntoView(lvAlreadyCopied.Items[lvAlreadyCopied.Items.Count - 1]);

            pbCopyProgress.Value = 0;

            MessageBox.Show($"Copied {successCount} file(s) successfully!", "Copy Complete",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            LoadFiles();
        }

        #endregion

        #region Context Menu: Open / Delete

        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu)
            {
                if (menu.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is ListView listView)
                {
                    foreach (var item in listView.SelectedItems.Cast<FileItem>())
                    {
                        string folderPath = listView == lvNewFiles ? txtSourcePath.Text : txtDestinationPath.Text;
                        string filePath = Path.Combine(folderPath, item.Name);

                        if (File.Exists(filePath))
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                                {
                                    FileName = filePath,
                                    UseShellExecute = true
                                });
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Cannot open file {item.Name}:\n{ex.Message}", "Error",
                                                MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
        }

        private void Menu_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu)
            {
                if (menu.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is ListView listView)
                {
                    var itemsToDelete = listView.SelectedItems.Cast<FileItem>().ToList();
                    if (itemsToDelete.Count == 0) return;

                    if (MessageBox.Show($"Are you sure you want to delete {itemsToDelete.Count} file(s)?",
                                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        return;

                    string folderPath = listView == lvNewFiles ? txtSourcePath.Text : txtDestinationPath.Text;

                    foreach (var fileItem in itemsToDelete)
                    {
                        string filePath = Path.Combine(folderPath, fileItem.Name);

                        try
                        {
                            if (File.Exists(filePath))
                                File.Delete(filePath);

                            listView.Items.Remove(fileItem);
                            LoadFiles();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to delete {fileItem.Name}:\n{ex.Message}", "Error",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
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


        #endregion
    }
}

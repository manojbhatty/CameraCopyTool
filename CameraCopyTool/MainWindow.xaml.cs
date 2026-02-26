using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using CameraCopyTool.Commands;
using CameraCopyTool.Models;
using CameraCopyTool.ViewModels;

namespace CameraCopyTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// The main window of the CameraCopyTool application.
    /// Handles UI events, ListView selection bindings, and context menu actions.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The main ViewModel for this window.
        /// Set as the DataContext for data binding.
        /// </summary>
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// Tracks the last sorted header for each ListView to clear its indicator.
        /// </summary>
        private readonly Dictionary<ListView, GridViewColumnHeader?> _lastSortedHeaders = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// Sets up data binding, event subscriptions, and UI initialization.
        /// </summary>
        /// <param name="viewModel">The MainViewModel instance, injected via DI.</param>
        public MainWindow(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;

            InitializeComponent();
            InitializeSelectionBindings();
            InitializeEventSubscriptions();
        }

        /// <summary>
        /// Initializes event subscriptions between the View and ViewModel.
        /// Subscribes to ViewModel events that require View interaction.
        /// </summary>
        private void InitializeEventSubscriptions()
        {
            // Subscribe to the OpenSettingsRequested event to show the Settings dialog
            _viewModel.OpenSettingsRequested += OnOpenSettingsRequested;

            // Subscribe to ListView SizeChanged events to adjust column widths dynamically
            lvAlreadyCopied.SizeChanged += ListView_SizeChanged;
            lvNewFiles.SizeChanged += ListView_SizeChanged;
            lvDestinationFiles.SizeChanged += ListView_SizeChanged;

            // Subscribe to ViewModel property changes for Copy button animation
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// Handles ViewModel property changes to update UI elements.
        /// Starts/stops Copy button pulse animation when files are ready to copy.
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.NewFiles) ||
                e.PropertyName == nameof(MainViewModel.SelectedNewFiles))
            {
                UpdateCopyButtonAnimation();
            }
        }

        /// <summary>
        /// Updates the Copy button pulse animation based on whether files are ready to copy.
        /// Animation plays when there are new files and at least one is selected.
        /// </summary>
        private void UpdateCopyButtonAnimation()
        {
            var storyboard = FindResource("CopyButtonPulse") as Storyboard;
            bool shouldAnimate = _viewModel.NewFiles.Count > 0 && _viewModel.SelectedNewFiles.Count > 0;

            if (shouldAnimate && storyboard != null)
            {
                CopyButton.BeginStoryboard(storyboard, HandoffBehavior.Compose, true);
            }
            else if (storyboard != null)
            {
                storyboard.Remove(CopyButton);
            }
        }

        /// <summary>
        /// Handles the SizeChanged event for ListViews to dynamically adjust column widths.
        /// The File Name column takes all remaining space after the Modified Date column.
        /// </summary>
        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is ListView listView && listView.View is GridView gridView &&
                gridView.Columns.Count == 2)
            {
                // Get the actual width of the Modified Date column (auto-sized to content)
                // If it's the first load, we need to let WPF calculate the auto width first
                var modifiedDateColumn = gridView.Columns[1];
                double modifiedDateWidth = modifiedDateColumn.ActualWidth;
                
                // If ActualWidth is 0 or NaN (not yet calculated), use a default minimum
                if (modifiedDateWidth <= 0 || double.IsNaN(modifiedDateWidth))
                {
                    modifiedDateWidth = 140;
                }
                
                // Calculate available width for File Name column
                // Subtract: Modified Date width + scroll bar width (~18) + small padding
                double fileNameWidth = Math.Max(100, listView.ActualWidth - modifiedDateWidth - 25);
                
                gridView.Columns[0].Width = fileNameWidth;
            }
        }

        /// <summary>
        /// Handles the OpenSettingsRequested event from the ViewModel.
        /// Creates and shows the SettingsWindow dialog.
        /// </summary>
        private void OnOpenSettingsRequested()
        {
            var settingsWindow = new Views.SettingsWindow(_viewModel);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        /// <summary>
        /// Initializes ListView selection bindings.
        /// Syncs selected items in ListViews with ViewModel properties.
        /// WPF ListView SelectedItems property is not bindable by default,
        /// so we use code-behind to keep the ViewModel updated.
        /// </summary>
        private void InitializeSelectionBindings()
        {
            // New Files ListView - selection affects Copy command availability
            lvNewFiles.SelectionChanged += (s, e) =>
            {
                _viewModel.SelectedNewFiles = lvNewFiles.SelectedItems.Cast<FileItem>().ToList();
                ((AsyncRelayCommand)_viewModel.CopyCommand).RaiseCanExecuteChanged();
            };

            // Already Copied Files ListView
            lvAlreadyCopied.SelectionChanged += (s, e) =>
            {
                _viewModel.SelectedAlreadyCopiedFiles = lvAlreadyCopied.SelectedItems.Cast<FileItem>().ToList();
            };

            // Destination Files ListView
            lvDestinationFiles.SelectionChanged += (s, e) =>
            {
                _viewModel.SelectedDestinationFiles = lvDestinationFiles.SelectedItems.Cast<FileItem>().ToList();
            };

            // Add toggle selection behavior - clicking selected item deselects it
            lvNewFiles.PreviewMouseLeftButtonDown += ListView_PreviewMouseLeftButtonDown;
            lvAlreadyCopied.PreviewMouseLeftButtonDown += ListView_PreviewMouseLeftButtonDown;
            lvDestinationFiles.PreviewMouseLeftButtonDown += ListView_PreviewMouseLeftButtonDown;
        }

        /// <summary>
        /// Handles mouse click on ListView items to enable toggle selection.
        /// Clicking an already selected item will deselect it.
        /// </summary>
        private void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView)
            {
                var item = FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject);
                if (item != null && item.IsSelected)
                {
                    // Item is already selected - clicking will deselect it
                    item.IsSelected = false;
                    e.Handled = true; // Prevent default selection behavior
                }
            }
        }

        /// <summary>
        /// Finds the first ancestor of the specified type in the visual tree.
        /// </summary>
        private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T typedChild)
                    return typedChild;
                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        /// <summary>
        /// Handles the Open menu item click from the context menu.
        /// Opens selected files with their default applications.
        /// </summary>
        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenCommand.Execute(null);
        }

        /// <summary>
        /// Handles the Delete menu item click from the context menu.
        /// Deletes selected files after confirmation.
        /// </summary>
        private async void Menu_Delete_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.DeleteCommand.ExecuteAsync(null);
        }

        /// <summary>
        /// Handles the Upload to Google Drive menu item click from the context menu.
        /// Shows a placeholder message for now (Issue #1 - Context Menu Infrastructure).
        /// </summary>
        private void Menu_UploadToGoogleDrive_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder implementation for Issue #1
            // Full implementation will be added in Issues #2-#6
            MessageBox.Show(
                "Google Drive upload feature is coming soon!\n\n" +
                "This feature will allow you to upload files directly to your Google Drive account.",
                "Upload to Google Drive",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Handles right-click on ListView items.
        /// Selects the item and gives it focus so context menu actions apply to it.
        /// </summary>
        private void ListViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item)
            {
                item.IsSelected = true;
                item.Focus();
            }
        }

        /// <summary>
        /// Handles click on GridView column headers for sorting.
        /// Toggles between ascending and descending sort order.
        /// Ignores clicks on the resize Thumb (PART_HeaderGrip).
        /// </summary>
        private void GridViewColumnHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Ignore clicks on the resize Thumb - let it handle dragging
            // Check if click was on the Thumb (resize grip)
            if (e.OriginalSource is Thumb || FindParent<Thumb>(e.OriginalSource as DependencyObject) != null)
            {
                return;
            }

            if (sender is GridViewColumnHeader header && header.Column != null)
            {
                string sortBy = header.Column.Header?.ToString() ?? "";
                if (string.IsNullOrEmpty(sortBy)) return;

                // Determine which ListView this header belongs to
                ListView? listView = null;
                ICollectionView? view = null;

                if (lvAlreadyCopied.IsLoaded && IsHeaderInListView(lvAlreadyCopied, header))
                {
                    listView = lvAlreadyCopied;
                    view = CollectionViewSource.GetDefaultView(_viewModel.AlreadyCopiedFiles);
                }
                else if (lvNewFiles.IsLoaded && IsHeaderInListView(lvNewFiles, header))
                {
                    listView = lvNewFiles;
                    view = CollectionViewSource.GetDefaultView(_viewModel.NewFiles);
                }
                else if (lvDestinationFiles.IsLoaded && IsHeaderInListView(lvDestinationFiles, header))
                {
                    listView = lvDestinationFiles;
                    view = CollectionViewSource.GetDefaultView(_viewModel.DestinationFiles);
                }

                if (view == null) return;

                // Toggle sort direction
                ListSortDirection direction = ListSortDirection.Ascending;
                if (view.SortDescriptions.Count > 0)
                {
                    var currentSort = view.SortDescriptions[0];
                    if (currentSort.PropertyName == GetPropertyName(sortBy) && currentSort.Direction == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                }

                // Apply sort
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(GetPropertyName(sortBy), direction));

                // Update the clicked header's indicator and clear the previous one
                if (listView != null)
                {
                    UpdateHeaderIndicator(listView, header, direction);
                }

                // Mark event as handled to prevent selection changes
                e.Handled = true;
            }
        }

        /// <summary>
        /// Updates the sort indicator on a specific header.
        /// </summary>
        private void UpdateHeaderIndicator(ListView listView, GridViewColumnHeader header, ListSortDirection direction)
        {
            // Clear the indicator from the previously sorted header in this ListView
            if (_lastSortedHeaders.TryGetValue(listView, out var lastHeader) && lastHeader != null)
            {
                var lastIndicator = FindChild<TextBlock>(lastHeader, "SortIndicator");
                if (lastIndicator != null)
                {
                    lastIndicator.Visibility = System.Windows.Visibility.Collapsed;
                }
            }

            // Set indicator on the clicked header
            var clickedIndicator = FindChild<TextBlock>(header, "SortIndicator");
            if (clickedIndicator != null)
            {
                clickedIndicator.Text = direction == ListSortDirection.Ascending ? "▲" : "▼";
                clickedIndicator.Visibility = System.Windows.Visibility.Visible;
            }

            // Track this as the last sorted header
            _lastSortedHeaders[listView] = header;
        }

        /// <summary>
        /// Finds a child element by name in the visual tree.
        /// </summary>
        private static T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe && fe.Name == childName)
                    return child as T;

                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// Checks if a header belongs to a specific ListView.
        /// </summary>
        private bool IsHeaderInListView(ListView listView, GridViewColumnHeader header)
        {
            if (listView.View is GridView gridView)
            {
                return gridView.Columns.Contains(header.Column);
            }
            return false;
        }

        /// <summary>
        /// Maps column header text to property name for sorting.
        /// </summary>
        private string GetPropertyName(string header)
        {
            return header switch
            {
                "File Name" => nameof(FileItem.DisplayName),
                "Modified Date" => nameof(FileItem.ModifiedDate),
                _ => header
            };
        }

        /// <summary>
        /// Handles mouse down on resize thumb - prevents sort from triggering.
        /// </summary>
        private void Thumb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Mark as handled to prevent sort from triggering when clicking the resize grip
            e.Handled = true;
        }

        /// <summary>
        /// Finds a parent element of the specified type in the visual tree.
        /// </summary>
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// Called when the window is closing.
        /// Notifies the ViewModel to save settings and clean up resources.
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            _viewModel.OnClosing();
            base.OnClosing(e);
        }
    }

    /// <summary>
    /// Extension methods for ICommand to support async execution.
    /// Provides a consistent way to execute both sync and async commands.
    /// </summary>
    public static class CommandExtensions
    {
        /// <summary>
        /// Executes an ICommand asynchronously.
        /// For AsyncRelayCommand, calls Execute directly (which is async void).
        /// For regular commands, checks CanExecute before executing.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="parameter">The command parameter.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task ExecuteAsync(this ICommand command, object? parameter)
        {
            if (command is AsyncRelayCommand asyncCommand)
            {
                asyncCommand.Execute(parameter);
            }
            else if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }
    }
}

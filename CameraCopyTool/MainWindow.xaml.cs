using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
        /// Tracks the last clicked GridView column header for sorting.
        /// (Currently unused - reserved for future sorting functionality)
        /// </summary>
        private GridViewColumnHeader? _lastHeaderClicked;

        /// <summary>
        /// Tracks the last sort direction for column sorting.
        /// (Currently unused - reserved for future sorting functionality)
        /// </summary>
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

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

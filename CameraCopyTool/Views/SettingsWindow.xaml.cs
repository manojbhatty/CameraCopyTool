using System.Windows;
using System.Windows.Controls;
using CameraCopyTool.ViewModels;

namespace CameraCopyTool.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml.
/// Provides a dialog for users to configure application settings,
/// primarily the font size for accessibility.
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>
    /// The ViewModel that owns these settings.
    /// Used to update the FontSize property when the user clicks OK.
    /// </summary>
    private readonly MainViewModel _viewModel;

    /// <summary>
    /// Stores the original font size when the dialog was opened.
    /// (Currently unused - reserved for potential cancel/revert functionality)
    /// </summary>
    private readonly double _originalFontSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
    /// Sets up data binding and initializes the preview text with the current font size.
    /// </summary>
    /// <param name="viewModel">The MainViewModel instance, passed from MainWindow.</param>
    public SettingsWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        _originalFontSize = viewModel.FontSize;

        InitializeComponent();
        DataContext = viewModel;

        // Initialize preview text with current font size
        PreviewText.FontSize = viewModel.FontSize;
    }

    /// <summary>
    /// Handles the slider value change event.
    /// Updates the preview text font size in real-time as the user adjusts the slider.
    /// This provides immediate visual feedback for the font size setting.
    /// </summary>
    /// <param name="sender">The slider control.</param>
    /// <param name="e">Event data containing the old and new values.</param>
    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PreviewText != null)
        {
            PreviewText.FontSize = e.NewValue;
        }
    }

    /// <summary>
    /// Handles the OK button click.
    /// Saves the selected font size to the ViewModel and closes the dialog.
    /// The ViewModel persists the setting to storage.
    /// </summary>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.FontSize = FontSizeSlider.Value;
        _viewModel.OnClosing(); // Save settings to persistent storage
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Handles the Reset button click.
    /// Resets the font size to the default value (20) and updates the preview.
    /// </summary>
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        FontSizeSlider.Value = 20;
        _viewModel.FontSize = 20;
        PreviewText.FontSize = 20;
    }
}

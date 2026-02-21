using System.Windows;
using System.Windows.Controls;

namespace CameraCopyTool.Views;

/// <summary>
/// Interaction logic for DeleteConfirmationDialog.xaml.
/// Provides a custom-styled confirmation dialog for delete operations
/// with clear warning messaging for senior-friendly UX.
/// </summary>
public partial class DeleteConfirmationDialog : Window
{
    /// <summary>
    /// Gets the user's response to the delete confirmation.
    /// </summary>
    public bool Confirmed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteConfirmationDialog"/> class.
    /// </summary>
    /// <param name="message">The warning message to display.</param>
    /// <param name="fontSize">The font size to use for the message text and buttons.</param>
    public DeleteConfirmationDialog(string message, double fontSize = 14)
    {
        InitializeComponent();
        MessageText.Text = message;
        MessageText.FontSize = fontSize;
        
        // Apply font size to buttons for consistency with rest of application
        YesButton.FontSize = fontSize;
        NoButton.FontSize = fontSize;
        
        // Adjust window height based on font size for larger fonts
        if (fontSize > 16)
        {
            Height = 300;
        }
        if (fontSize > 20)
        {
            Height = 340;
        }
    }

    /// <summary>
    /// Handles the Yes button click.
    /// Sets Confirmed to true and closes the dialog.
    /// </summary>
    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Handles the No button click.
    /// Sets Confirmed to false and closes the dialog.
    /// </summary>
    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }
}

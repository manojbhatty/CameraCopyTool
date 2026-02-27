using System.Windows;

namespace CameraCopyTool.Views
{
    /// <summary>
    /// Interaction logic for GoogleDriveAuthDialog.xaml
    /// </summary>
    public partial class GoogleDriveAuthDialog : Window
    {
        public GoogleDriveAuthDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets a value indicating whether the user wants to proceed with authentication.
        /// </summary>
        public bool UserConsentGiven { get; private set; }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            UserConsentGiven = true;
            DialogResult = true;
            // Note: We don't close here - the caller will keep the dialog open
            // and show "Opening browser..." message
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UserConsentGiven = false;
            DialogResult = false;
            Close();
        }
    }
}

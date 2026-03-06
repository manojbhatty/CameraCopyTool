using System.Windows;

namespace CameraCopyTool.Views
{
    /// <summary>
    /// Interaction logic for GoogleDriveAuthDialog.xaml
    /// </summary>
    public partial class GoogleDriveAuthDialog : Window
    {
        public GoogleDriveAuthDialog(double fontSize = 20)
        {
            InitializeComponent();
            
            // Apply font size to all text elements
            CloudEmojiText.FontSize = fontSize * 2.5;  // ~50px at base 20
            TitleText.FontSize = fontSize * 1.2;       // ~24px at base 20
            SubtitleText.FontSize = fontSize * 0.8;    // ~16px at base 20
            IntroText.FontSize = fontSize * 0.85;      // ~17px at base 20
            HowItWorksText.FontSize = fontSize * 0.85; // ~17px at base 20
            Step1Text.FontSize = fontSize * 0.85;      // ~17px at base 20
            Step2Text.FontSize = fontSize * 0.85;      // ~17px at base 20
            Step3Text.FontSize = fontSize * 0.85;      // ~17px at base 20
            Step4Text.FontSize = fontSize * 0.85;      // ~17px at base 20
            Step5Text.FontSize = fontSize * 0.85;      // ~17px at base 20
            InfoText.FontSize = fontSize * 0.8;        // ~16px at base 20
            
            // Apply font size to buttons
            SignInButton.FontSize = fontSize * 1.05;
            CancelButton.FontSize = fontSize;
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

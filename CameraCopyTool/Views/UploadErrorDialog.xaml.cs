using System;
using System.Windows;
using System.Windows.Media;
using CameraCopyTool.Models;

namespace CameraCopyTool.Views
{
    /// <summary>
    /// Result of the upload error dialog.
    /// </summary>
    public enum UploadErrorResult
    {
        Retry,
        Pause,
        Cancel
    }

    public partial class UploadErrorDialog : Window
    {
        private readonly UploadError _error;
        private UploadErrorResult _result;

        public UploadErrorDialog(UploadError error, Window owner)
        {
            InitializeComponent();
            Owner = owner;
            _error = error;
            _result = UploadErrorResult.Cancel;

            // Set icon and colors based on error category
            SetErrorAppearance();

            // Set title and message
            ErrorTitle.Text = _error.GetTitle();
            ErrorMessage.Text = _error.GetUserMessage();

            // Show/hide retry button based on error type
            RetryButton.Visibility = _error.IsRetryable ? Visibility.Visible : Visibility.Collapsed;
        }

        public UploadErrorResult Result => _result;

        private void SetErrorAppearance()
        {
            switch (_error.Category)
            {
                case ErrorCategory.Network:
                    ErrorIcon.Text = "🌐";
                    ErrorTitle.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    break;

                case ErrorCategory.Api:
                    ErrorIcon.Text = "⚠️";
                    ErrorTitle.Foreground = new SolidColorBrush(Color.FromRgb(255, 87, 34));
                    break;

                case ErrorCategory.File:
                    ErrorIcon.Text = "📁";
                    ErrorTitle.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    break;

                case ErrorCategory.Authentication:
                    ErrorIcon.Text = "🔐";
                    ErrorTitle.Foreground = new SolidColorBrush(Color.FromRgb(255, 87, 34));
                    break;

                default:
                    ErrorIcon.Text = "⚠️";
                    ErrorTitle.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    break;
            }
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            _result = UploadErrorResult.Retry;
            DialogResult = true;
            Close();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _result = UploadErrorResult.Pause;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result = UploadErrorResult.Cancel;
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Shows the retrying progress indicator.
        /// </summary>
        public void ShowRetrying()
        {
            RetryProgressPanel.Visibility = Visibility.Visible;
            WaitingIndicator.Visibility = Visibility.Collapsed;
            RetryButton.IsEnabled = false;
            PauseButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
        }

        /// <summary>
        /// Updates the dialog for paused state.
        /// </summary>
        public void ShowPaused()
        {
            ErrorIcon.Text = "⏸️";
            ErrorTitle.Text = "Upload Paused";
            ErrorMessage.Text = "The upload has been paused. Click 'Retry Now' to resume when you're ready.";
            PauseButton.Visibility = Visibility.Collapsed;
            RetryButton.IsEnabled = true;
        }
    }
}

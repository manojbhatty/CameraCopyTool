using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CameraCopyTool.Models;

namespace CameraCopyTool.Views
{
    /// <summary>
    /// Interaction logic for GoogleDriveUploadProgressDialog.xaml
    /// </summary>
    public partial class GoogleDriveUploadProgressDialog : Window, IProgress<UploadProgress>
    {
        private bool _isCancelled;
        private bool _isCompleted;

        public GoogleDriveUploadProgressDialog(string fileName, double fontSize = 20)
        {
            InitializeComponent();
            FileNameText.Text = fileName;
            _isCancelled = false;
            _isCompleted = false;
            
            // Apply font size immediately to all elements
            this.FontSize = fontSize;
            FileNameText.FontSize = fontSize;
            PercentageText.FontSize = fontSize;
            // SpeedText removed
            TimeText.FontSize = fontSize;
            CancelButton.FontSize = fontSize;
            OkButton.FontSize = fontSize;
        }

        /// <summary>
        /// Gets a value indicating whether the upload was cancelled.
        /// </summary>
        public bool IsCancelled => _isCancelled;

        /// <summary>
        /// Gets a value indicating whether the upload completed successfully.
        /// </summary>
        public bool IsCompleted => _isCompleted;

        /// <summary>
        /// Reports progress update.
        /// </summary>
        public void Report(UploadProgress progress)
        {
            Dispatcher.Invoke(() =>
            {
                // Ensure progress bar updates correctly
                var percentage = progress.Percentage;
                if (percentage < 0) percentage = 0;
                if (percentage > 100) percentage = 100;
                
                // Explicitly set and refresh progress bar
                UploadProgressBar.Value = percentage;
                UploadProgressBar.UpdateLayout();
                
                PercentageText.Text = $"{percentage:F1}%";
                // Speed removed
                TimeText.Text = progress.TimeRemainingString;
                
                // Auto-complete on 100%
                if (percentage >= 100.0 && !_isCompleted)
                {
                    SetCompleted();
                }
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isCancelled = true;
            DialogResult = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _isCompleted = true;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Sets the dialog to completed state.
        /// </summary>
        public void SetCompleted()
        {
            _isCompleted = true;
            
            Dispatcher.Invoke(() =>
            {
                UploadProgressBar.Value = 100;
                PercentageText.Text = "✓ Upload Complete!";
                // SpeedText removed
                TimeText.Text = "";
                
                // Hide Cancel button, show OK button
                CancelButton.Visibility = Visibility.Collapsed;
                OkButton.Visibility = Visibility.Visible;
                OkButton.Focus();
            });
        }
    }
}

using System;
using System.Windows;
using System.Windows.Media;
using CameraCopyTool.Models;

namespace CameraCopyTool.Views
{
    public partial class GoogleDriveUploadProgressDialog : Window, IProgress<UploadProgress>
    {
        private bool _isCancelled;
        private bool _isCompleted;
        private DateTime _startTime;

        public GoogleDriveUploadProgressDialog(string fileName, long fileSize, double fontSize = 20)
        {
            InitializeComponent();
            FileNameText.Text = fileName;
            FileSizeText.Text = FormatFileSize(fileSize);
            _isCancelled = false;
            _isCompleted = false;
            _startTime = DateTime.Now;

            // Apply font size to all elements
            TitleText.FontSize = fontSize * 1.1;        // ~22px at base 20
            StatusMessage.FontSize = fontSize * 0.8;    // ~16px at base 20
            FileNameLabel.FontSize = fontSize * 0.6;    // ~12px at base 20
            FileNameText.FontSize = fontSize * 0.8;     // ~16px at base 20
            SizeLabel.FontSize = fontSize * 0.6;        // ~12px at base 20
            FileSizeText.FontSize = fontSize * 0.8;     // ~16px at base 20
            PercentageLabel.FontSize = fontSize * 0.6;  // ~12px at base 20
            TimeLabel.FontSize = fontSize * 0.6;        // ~12px at base 20
            TimeText.FontSize = fontSize * 0.75;        // ~15px at base 20
            CancelButton.FontSize = fontSize;
            OkButton.FontSize = fontSize;

            // Start with 0%
            UploadProgressBar.Value = 0;
            PercentageText.Text = "0%";
            TimeText.Text = "Starting...";
        }

        public bool IsCancelled => _isCancelled;
        public bool IsCompleted => _isCompleted;

        public void Report(UploadProgress progress)
        {
            Dispatcher.Invoke(() =>
            {
                double percentage = progress.Percentage;
                if (percentage < 0) percentage = 0;
                if (percentage > 100) percentage = 100;

                // Update progress bar
                UploadProgressBar.Value = percentage;

                // Update percentage text
                string percentageStr = $"{percentage:F0}%";
                PercentageText.Text = percentageStr;

                // Update status message based on progress
                if (percentage < 10)
                {
                    StatusMessage.Text = "Starting upload... please wait";
                }
                else if (percentage < 50)
                {
                    StatusMessage.Text = "Uploading... please wait";
                }
                else if (percentage < 90)
                {
                    StatusMessage.Text = "Making good progress...";
                }
                else if (percentage < 100)
                {
                    StatusMessage.Text = "Almost done...";
                }

                // Calculate time remaining
                if (progress.BytesSent > 0 && progress.TotalBytes > 0 && percentage < 100)
                {
                    var elapsedSeconds = (DateTime.Now - _startTime).TotalSeconds;

                    if (elapsedSeconds > 0.1)
                    {
                        var bytesPerSecond = progress.BytesSent / elapsedSeconds;
                        var remainingBytes = progress.TotalBytes - progress.BytesSent;

                        if (bytesPerSecond > 0 && remainingBytes > 0)
                        {
                            var remainingSeconds = remainingBytes / bytesPerSecond;
                            TimeText.Text = FormatTimeRemaining(remainingSeconds);
                        }
                        else
                        {
                            TimeText.Text = "Calculating...";
                        }
                    }
                    else
                    {
                        TimeText.Text = "Starting...";
                    }
                }
                else if (percentage >= 100)
                {
                    TimeText.Text = "Complete!";
                    
                    // Call SetCompleted to show success state
                    if (!_isCompleted)
                    {
                        SetCompleted();
                    }
                }
                else
                {
                    TimeText.Text = "Starting...";
                }
            });
        }

        private string FormatTimeRemaining(double seconds)
        {
            if (seconds < 60)
            {
                return $"{(int)seconds} seconds";
            }
            else if (seconds < 3600)
            {
                var minutes = (int)(seconds / 60);
                var secs = (int)(seconds % 60);
                return $"{minutes}m {secs}s";
            }
            else
            {
                var hours = (int)(seconds / 3600);
                var minutes = (int)((seconds % 3600) / 60);
                return $"{hours}h {minutes}m";
            }
        }
        
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isCancelled = true;
            
            // Change icon to orange warning
            StatusIcon.Text = "⚠️";
            StatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
            
            // Update title
            TitleText.Text = "Upload Cancelled";
            TitleText.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
            
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
                // Change icon from cloud to green checkmark
                StatusIcon.Text = "✅";
                StatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                
                // Update title to show success
                TitleText.Text = "Upload Success!";
                TitleText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                
                UploadProgressBar.Value = 100;
                PercentageText.Text = "100%";
                TimeText.Text = "✓ Upload Success!";
                TimeText.Foreground = new SolidColorBrush(Colors.Green);

                // Hide Cancel button, show OK button
                CancelButton.Visibility = Visibility.Collapsed;
                OkButton.Visibility = Visibility.Visible;
                OkButton.Focus();
                
                // Play success sound
                System.Media.SystemSounds.Asterisk.Play();
            });
        }
    }
}

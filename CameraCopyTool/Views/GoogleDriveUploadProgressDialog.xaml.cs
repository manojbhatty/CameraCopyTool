using System;
using System.Threading.Tasks;
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
        private UploadError? _currentError;
        private bool _isPaused;
        private TaskCompletionSource<bool>? _pauseCompletionSource;

        public GoogleDriveUploadProgressDialog(string fileName, long fileSize, double fontSize = 20)
        {
            InitializeComponent();
            FileNameText.Text = fileName;
            FileSizeText.Text = FormatFileSize(fileSize);
            _isCancelled = false;
            _isCompleted = false;
            _isPaused = false;
            _startTime = DateTime.Now;

            // Apply font size to all elements
            TitleText.FontSize = fontSize * 1.1;        // ~22px at base 20
            StatusMessage.FontSize = fontSize * 0.8;    // ~16px at base 20
            FileNameLabel.FontSize = fontSize * 0.7;    // ~12px at base 20
            FileNameText.FontSize = fontSize * 0.9;     // ~16px at base 20
            SizeLabel.FontSize = fontSize * 0.7;        // ~12px at base 20
            FileSizeText.FontSize = fontSize * 0.9;     // ~16px at base 20
            // PercentageLabel.FontSize = fontSize * 0.6;  // ~12px at base 20
            TimeLabel.FontSize = fontSize * 0.8;        // ~12px at base 20
            TimeText.FontSize = fontSize;        // ~15px at base 20
            ReassuranceText.FontSize = fontSize * 0.8;  // ~14px at base 20
            CancelButton.FontSize = fontSize;
            OkButton.FontSize = fontSize;

            // Start with 0%
            UploadProgressBar.Value = 0;
            // PercentageText.Text = "0%";
            TimeText.Text = "Starting...";
        }

        public bool IsCancelled => _isCancelled;
        public bool IsCompleted => _isCompleted;
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Shows an error dialog or status message based on error type.
        /// For network errors, shows a status message and auto-retries.
        /// For other errors, shows a dialog for user action.
        /// </summary>
        /// <param name="error">The upload error.</param>
        /// <param name="retryCount">Current retry attempt (for non-network errors).</param>
        /// <param name="maxRetries">Maximum retry attempts.</param>
        /// <returns>True if the upload should retry, false otherwise.</returns>
        public async Task<bool> ShowErrorAsync(UploadError error, int retryCount = 0, int maxRetries = 5)
        {
            _currentError = error;

            // For network errors, show status message and auto-retry
            if (error.Category == ErrorCategory.Network)
            {
                ShowNetworkWaiting();
                return true; // Auto-retry for network errors
            }

            // For other retryable errors, show retry status
            if (error.IsRetryable && retryCount < maxRetries)
            {
                ShowRetryStatus(retryCount + 1, maxRetries);
                return true; // Auto-retry
            }

            // For non-retryable or max retries reached, show dialog
            var tcs = new TaskCompletionSource<bool>();
            _pauseCompletionSource = tcs;

            // Show error dialog on UI thread
            await Dispatcher.InvokeAsync(() =>
            {
                var dialog = new UploadErrorDialog(error, this);
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    switch (dialog.Result)
                    {
                        case UploadErrorResult.Retry:
                            tcs.SetResult(true);
                            break;
                        case UploadErrorResult.Pause:
                            dialog.ShowPaused();
                            _isPaused = true;
                            // Wait for resume or cancel
                            Task.Run(async () =>
                            {
                                await Task.Delay(1000); // Give time for UI to update
                                if (_pauseCompletionSource != null && !_pauseCompletionSource.Task.IsCompleted)
                                {
                                    // User chose pause - wait indefinitely for resume/cancel
                                    while (_isPaused && !_isCancelled)
                                    {
                                        await Task.Delay(500);
                                    }
                                    if (!_isCancelled)
                                    {
                                        tcs.SetResult(true); // Resume
                                    }
                                }
                            });
                            break;
                        case UploadErrorResult.Cancel:
                            _isCancelled = true;
                            tcs.SetResult(false);
                            break;
                    }
                }
                else
                {
                    _isCancelled = true;
                    tcs.SetResult(false);
                }
            });

            return await tcs.Task;
        }

        /// <summary>
        /// Resumes the upload after being paused.
        /// </summary>
        public void ResumeUpload()
        {
            _isPaused = false;
            _pauseCompletionSource?.TrySetResult(true);
            
            Dispatcher.Invoke(() =>
            {
                StatusMessage.Text = "Resuming upload...";
                StatusMessage.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                StatusIcon.Text = "☁️";
                StatusIcon.Foreground = new SolidColorBrush(Colors.Black);
                TitleText.Text = "Uploading to Google Drive";
                TitleText.Foreground = new SolidColorBrush(Colors.Black);
            });
        }

        /// <summary>
        /// Shows network waiting status.
        /// </summary>
        public void ShowNetworkWaiting()
        {
            Dispatcher.Invoke(() =>
            {
                StatusMessage.Text = "No internet connection. Waiting to resume...";
                StatusMessage.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                StatusIcon.Text = "🌐";
                StatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                ReassuranceText.Text = "⏳ Upload will automatically resume when connection is restored";
                ReassuranceText.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
            });
        }

        /// <summary>
        /// Shows retry status with countdown.
        /// </summary>
        /// <param name="retryCount">Current retry attempt.</param>
        /// <param name="maxRetries">Maximum retry attempts.</param>
        public void ShowRetryStatus(int retryCount, int maxRetries)
        {
            Dispatcher.Invoke(() =>
            {
                StatusMessage.Text = $"Retrying upload... (attempt {retryCount} of {maxRetries})";
                StatusMessage.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                ReassuranceText.Text = "⏳ Please wait, retrying automatically";
                ReassuranceText.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
            });
        }

        /// <summary>
        /// Shows the file conflict dialog.
        /// </summary>
        /// <param name="fileName">Name of the conflicting file.</param>
        /// <param name="fileSize">Size of the file.</param>
        /// <returns>The user's choice.</returns>
        public FileConflictResult ShowFileConflictDialog(string fileName, long fileSize)
        {
            var dialog = new FileConflictDialog(fileName, fileSize, this);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                return dialog.Result;
            }

            return FileConflictResult.Cancel;
        }

        public void Report(UploadProgress progress)
        {
            Dispatcher.Invoke(() =>
            {
                double percentage = progress.Percentage;
                if (percentage < 0) percentage = 0;
                if (percentage > 100) percentage = 100;

                // Update progress bar
                UploadProgressBar.Value = percentage;
                
                // Update percentage text inside progress bar
                ProgressPercentageText.Text = $"{percentage:F0}%";

                // Update percentage text below (keep for consistency)
                // PercentageText.Text = $"{percentage:F0}%";

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
                
                // Update status message to success
                StatusMessage.Text = "✓ Your file is safe on Google Drive!";
                StatusMessage.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));

                UploadProgressBar.Value = 100;
                // PercentageText.Text = "100%";
                TimeText.Text = "✓ Upload Success!";
                TimeText.Foreground = new SolidColorBrush(Colors.Green);
                
                // Update reassurance text to success message
                ReassuranceText.Text = "✓ Upload successful! You can now close this window.";
                ReassuranceText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));

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

using System;
using System.Windows;

namespace CameraCopyTool.Views
{
    /// <summary>
    /// Interaction logic for GoogleDriveUploadProgressDialog.xaml
    /// </summary>
    public partial class GoogleDriveUploadProgressDialog : Window, IProgress<double>
    {
        private bool _isCancelled;

        public GoogleDriveUploadProgressDialog(string fileName)
        {
            InitializeComponent();
            FileNameText.Text = fileName;
            _isCancelled = false;
        }

        /// <summary>
        /// Gets a value indicating whether the upload was cancelled.
        /// </summary>
        public bool IsCancelled => _isCancelled;

        /// <summary>
        /// Reports progress update.
        /// </summary>
        public void Report(double value)
        {
            if (value < 0) value = 0;
            if (value > 100) value = 100;

            Dispatcher.Invoke(() =>
            {
                UploadProgressBar.Value = value;
                ProgressText.Text = $"{value:F1}%";
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isCancelled = true;
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Sets the dialog to completed state.
        /// </summary>
        public void SetCompleted()
        {
            Dispatcher.Invoke(() =>
            {
                UploadProgressBar.Value = 100;
                ProgressText.Text = "100% - Complete!";
            });
        }
    }
}

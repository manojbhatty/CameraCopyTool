using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CameraCopyTool.Views
{
    /// <summary>
    /// Result of the file conflict dialog.
    /// </summary>
    public enum FileConflictResult
    {
        Replace,
        KeepBoth,
        Skip,
        Cancel
    }

    public partial class FileConflictDialog : Window
    {
        private FileConflictResult _result;

        public FileConflictDialog(string fileName, long fileSize, Window owner)
        {
            InitializeComponent();
            Owner = owner;
            _result = FileConflictResult.Cancel;

            // Set file information
            FileNameText.Text = fileName;
            FileSizeText.Text = FormatFileSize(fileSize);
        }

        public FileConflictResult Result => _result;

        private void ReplaceOption_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ReplaceRadio.IsChecked = true;
            UpdateOptionSelection();
        }

        private void KeepBothOption_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            KeepBothRadio.IsChecked = true;
            UpdateOptionSelection();
        }

        private void SkipOption_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SkipRadio.IsChecked = true;
            UpdateOptionSelection();
        }

        private void UpdateOptionSelection()
        {
            // Reset all borders
            ReplaceOption.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            KeepBothOption.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            SkipOption.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));

            // Highlight selected option
            if (ReplaceRadio.IsChecked == true)
            {
                ReplaceOption.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            }
            else if (KeepBothRadio.IsChecked == true)
            {
                KeepBothOption.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            }
            else if (SkipRadio.IsChecked == true)
            {
                SkipOption.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReplaceRadio.IsChecked == true)
            {
                _result = FileConflictResult.Replace;
            }
            else if (KeepBothRadio.IsChecked == true)
            {
                _result = FileConflictResult.KeepBoth;
            }
            else if (SkipRadio.IsChecked == true)
            {
                _result = FileConflictResult.Skip;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result = FileConflictResult.Cancel;
            DialogResult = false;
            Close();
        }

        private static string FormatFileSize(long bytes)
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
    }
}

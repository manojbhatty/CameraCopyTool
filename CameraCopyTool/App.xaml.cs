using System.Windows;

namespace CameraCopyTool
{
    public partial class App : Application
    {
        // Usually nothing is needed here if you use StartupUri
        // In App.xaml.cs
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var main = new MainWindow();
                main.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

    }
}

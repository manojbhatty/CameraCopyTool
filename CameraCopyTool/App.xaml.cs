using System.IO;
using System.Text;
using System.Windows;
using CameraCopyTool.Services;
using CameraCopyTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CameraCopyTool
{
    /// <summary>
    /// The main application class for CameraCopyTool.
    /// Handles application startup, dependency injection configuration, and service registration.
    /// Inherits from WPF's Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// The service provider for dependency injection.
        /// Used to resolve services and ViewModels throughout the application.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// Configures dependency injection and builds the service provider.
        /// </summary>
        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Subscribe to global exception events
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        /// <summary>
        /// Configures the dependency injection container with application services.
        /// Registers all services, ViewModels, and Views used by the application.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private static void ConfigureServices(IServiceCollection services)
        {
            // Register core services as singletons (one instance for the application lifetime)
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<ISettingsService, SettingsService>();

            // Register settings loader
            services.AddSingleton<ISettingsLoader, AppSettingsLoader>();

            // Register Google Drive settings (loaded from App.config)
            services.AddSingleton(provider =>
            {
                var settingsLoader = provider.GetRequiredService<ISettingsLoader>();
                return settingsLoader.LoadGoogleDriveSettings();
            });

            // Register Google Drive service
            services.AddSingleton<IGoogleDriveService, GoogleDriveService>();

            // Register MainViewModel as transient (new instance each time)
            services.AddTransient<MainViewModel>();

            // Register MainWindow as transient (new instance each time)
            // MainWindow receives its dependencies (including MainViewModel) via constructor injection
            services.AddTransient<MainWindow>();
        }

        /// <summary>
        /// Logs an exception to a file in the application's data directory.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="source">The source of the exception (e.g., "Dispatcher", "AppDomain").</param>
        private static void LogException(Exception ex, string source)
        {
            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CameraCopyTool",
                    "error.log");

                var directory = Path.GetDirectoryName(logPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var logEntry = new StringBuilder();
                logEntry.AppendLine($"=== {source} Exception ===");
                logEntry.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                logEntry.AppendLine($"Exception Type: {ex.GetType().FullName}");
                logEntry.AppendLine($"Message: {ex.Message}");
                logEntry.AppendLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    logEntry.AppendLine();
                    logEntry.AppendLine($"=== Inner Exception ===");
                    logEntry.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                    logEntry.AppendLine($"Message: {ex.InnerException.Message}");
                    logEntry.AppendLine($"Stack Trace: {ex.InnerException.StackTrace}");
                }

                logEntry.AppendLine();
                logEntry.AppendLine(new string('=', 80));
                logEntry.AppendLine();

                File.AppendAllText(logPath, logEntry.ToString());
            }
            catch
            {
                // Ignore logging errors to prevent infinite loops
            }
        }

        /// <summary>
        /// Handles unhandled exceptions from the AppDomain.
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex, "AppDomain");
            }
        }

        /// <summary>
        /// Handles unhandled exceptions from the WPF dispatcher.
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception, "Dispatcher");
            e.Handled = true; // Prevent application crash

            MessageBox.Show(
                $"An error occurred: {e.Exception.Message}\n\nDetails have been logged to the error log file.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// Called when the application starts.
        /// Creates and shows the main window using dependency injection.
        /// </summary>
        /// <param name="e">Event data for the startup event.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Resolve MainWindow from DI container
                // This ensures all dependencies are properly injected
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                // Log the exception
                LogException(ex, "Startup");

                // Show any startup errors to the user
                MessageBox.Show(ex.ToString(), "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets a service of the specified type from the service provider.
        /// Static helper method for accessing services from anywhere in the application.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>The requested service instance.</returns>
        public static T GetService<T>() where T : class
        {
            return ((App)Current)._serviceProvider.GetRequiredService<T>();
        }
    }
}

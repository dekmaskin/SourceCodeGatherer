using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using SourceCodeGatherer.Services;

namespace SourceCodeGatherer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ILoggingService _loggingService;
        private ILogger<App> _logger;

        /// <summary>
        /// Initializes a new instance of the App class.
        /// </summary>
        public App()
        {
            InitializeLogging();
            SetupGlobalExceptionHandling();
        }

        /// <summary>
        /// Gets the logging service instance.
        /// </summary>
        public static ILoggingService LoggingService { get; private set; }

        /// <summary>
        /// Handles the application startup event.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            _logger.LogInformation("Application startup initiated");
            _loggingService.LogApplicationStart();
            
            if (e.Args.Length > 0)
            {
                _logger.LogInformation("Command line arguments: {Args}", string.Join(" ", e.Args));
            }

            // Perform log cleanup based on settings
            await PerformLogCleanupAsync();

            base.OnStartup(e);
            _logger.LogInformation("Application startup completed");
        }

        /// <summary>
        /// Handles the application exit event.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            _logger.LogInformation("Application exit initiated with code: {ExitCode}", e.ApplicationExitCode);
            _loggingService.LogApplicationShutdown();
            
            base.OnExit(e);
            
            // Dispose logging service to flush logs
            if (_loggingService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Initializes the logging service.
        /// </summary>
        private void InitializeLogging()
        {
            try
            {
                _loggingService = new LoggingService();
                LoggingService = _loggingService; // Make available globally
                _logger = _loggingService.GetLogger<App>();
                _logger.LogDebug("Logging service initialized successfully");
            }
            catch (Exception ex)
            {
                // If logging fails, we can't log the error, so show a message box
                MessageBox.Show($"Failed to initialize logging: {ex.Message}", "Logging Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Sets up global exception handling for unhandled exceptions.
        /// </summary>
        private void SetupGlobalExceptionHandling()
        {
            // Handle unhandled exceptions in the main UI thread
            DispatcherUnhandledException += (sender, e) =>
            {
                _logger?.LogError(e.Exception, "Unhandled dispatcher exception occurred");
                _loggingService?.LogUnhandledException(e.Exception);
                
                var result = MessageBox.Show(
                    $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nWould you like to continue running the application?",
                    "Unexpected Error",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                e.Handled = result == MessageBoxResult.Yes;
                
                if (!e.Handled)
                {
                    _logger?.LogInformation("User chose to exit application after unhandled exception");
                }
            };

            // Handle unhandled exceptions in background threads
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                _logger?.LogCritical(exception, "Unhandled domain exception occurred. Terminating: {IsTerminating}", e.IsTerminating);
                _loggingService?.LogUnhandledException(exception);

                if (e.IsTerminating)
                {
                    MessageBox.Show(
                        $"A fatal error occurred and the application must close:\n\n{exception?.Message}",
                        "Fatal Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Stop);
                }
            };
        }

        /// <summary>
        /// Performs log cleanup based on application settings.
        /// </summary>
        private async System.Threading.Tasks.Task PerformLogCleanupAsync()
        {
            try
            {
                // Load settings to get retention policy
                var settingsService = new SettingsService();
                var settings = await settingsService.LoadSettingsAsync();
                
                _logger.LogDebug("Performing log cleanup with retention policy: {RetentionDays} days", settings.LogRetentionDays);
                
                // Perform cleanup
                await _loggingService.CleanupOldLogsAsync(settings.LogRetentionDays);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to perform log cleanup during startup");
            }
        }
    }
}
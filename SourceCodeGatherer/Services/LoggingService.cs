using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SourceCodeGatherer.Models;

namespace SourceCodeGatherer.Services
{
    /// <summary>
    /// Service that provides application-wide logging capabilities using Serilog.
    /// </summary>
    public class LoggingService : ILoggingService, IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly Serilog.ILogger _serilogLogger;
        private readonly string _logDirectory;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the LoggingService class.
        /// </summary>
        public LoggingService()
        {
            // Configure Serilog
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SourceCodeGatherer",
                "Logs");

            Directory.CreateDirectory(_logDirectory);

            var logFilePath = Path.Combine(_logDirectory, "app-.log");

            // Use a large initial retention limit - we'll clean up manually based on settings
            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: null, // Disable automatic cleanup, we'll handle it manually
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
#if DEBUG
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
#endif
                .Enrich.FromLogContext()
                .CreateLogger();

            _loggerFactory = new SerilogLoggerFactory(_serilogLogger);

            // Set global Serilog logger
            Log.Logger = _serilogLogger;
        }

        /// <inheritdoc/>
        public ILogger<T> GetLogger<T>()
        {
            return _loggerFactory.CreateLogger<T>();
        }

        /// <inheritdoc/>
        public Microsoft.Extensions.Logging.ILogger GetLogger(string name)
        {
            return _loggerFactory.CreateLogger(name);
        }

        /// <inheritdoc/>
        public void LogApplicationStart()
        {
            _serilogLogger.Information("=== Source Code Gatherer Application Started ===");
            _serilogLogger.Information("Version: {Version}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            _serilogLogger.Information("OS: {OS}", Environment.OSVersion);
            _serilogLogger.Information("Runtime: {Runtime}", Environment.Version);
            _serilogLogger.Information("Working Directory: {WorkingDirectory}", Environment.CurrentDirectory);
        }

        /// <inheritdoc/>
        public void LogApplicationShutdown()
        {
            _serilogLogger.Information("=== Source Code Gatherer Application Shutdown ===");
        }

        /// <inheritdoc/>
        public void LogUnhandledException(Exception exception)
        {
            _serilogLogger.Fatal(exception, "Unhandled exception occurred in application");
        }

        /// <summary>
        /// Cleans up old log files based on the retention policy.
        /// </summary>
        /// <param name="retentionDays">Number of days to retain log files.</param>
        public async Task CleanupOldLogsAsync(int retentionDays)
        {
            try
            {
                if (retentionDays <= 0)
                {
                    _serilogLogger.Warning("Invalid retention days value: {RetentionDays}. Skipping log cleanup.", retentionDays);
                    return;
                }

                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                _serilogLogger.Debug("Starting log cleanup. Retention: {RetentionDays} days, Cutoff date: {CutoffDate}", retentionDays, cutoffDate);

                await Task.Run(() =>
                {
                    if (!Directory.Exists(_logDirectory))
                        return;

                    var logFiles = Directory.GetFiles(_logDirectory, "app-*.log")
                        .Where(file =>
                        {
                            try
                            {
                                var fileInfo = new FileInfo(file);
                                return fileInfo.CreationTime < cutoffDate;
                            }
                            catch
                            {
                                return false; // Skip files we can't access
                            }
                        })
                        .ToList();

                    var deletedCount = 0;
                    var errorCount = 0;

                    foreach (var file in logFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                            _serilogLogger.Debug("Deleted old log file: {LogFile}", Path.GetFileName(file));
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            _serilogLogger.Warning(ex, "Failed to delete old log file: {LogFile}", Path.GetFileName(file));
                        }
                    }

                    if (deletedCount > 0 || errorCount > 0)
                    {
                        _serilogLogger.Information("Log cleanup completed. Deleted: {DeletedCount}, Errors: {ErrorCount}, Retention: {RetentionDays} days",
                                                 deletedCount, errorCount, retentionDays);
                    }
                });
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "Error during log cleanup process");
            }
        }

        /// <summary>
        /// Disposes the logging service and flushes any pending log entries.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _loggerFactory?.Dispose();
                Log.CloseAndFlush();
                _disposed = true;
            }
        }
    }
}
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SourceCodeGatherer.Services
{
    /// <summary>
    /// Interface for logging service that provides application-wide logging capabilities.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Gets a logger for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to create a logger for.</typeparam>
        /// <returns>A logger instance.</returns>
        ILogger<T> GetLogger<T>();

        /// <summary>
        /// Gets a logger with the specified name.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <returns>A logger instance.</returns>
        Microsoft.Extensions.Logging.ILogger GetLogger(string name);

        /// <summary>
        /// Logs an application startup event.
        /// </summary>
        void LogApplicationStart();

        /// <summary>
        /// Logs an application shutdown event.
        /// </summary>
        void LogApplicationShutdown();

        /// <summary>
        /// Logs an unhandled exception.
        /// </summary>
        /// <param name="exception">The unhandled exception.</param>
        void LogUnhandledException(Exception exception);

        /// <summary>
        /// Cleans up old log files based on the retention policy.
        /// </summary>
        /// <param name="retentionDays">Number of days to retain log files.</param>
        /// <returns>A task representing the cleanup operation.</returns>
        Task CleanupOldLogsAsync(int retentionDays);
    }
}
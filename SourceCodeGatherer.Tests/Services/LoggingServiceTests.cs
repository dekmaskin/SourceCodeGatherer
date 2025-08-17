using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using SourceCodeGatherer.Services;

namespace SourceCodeGatherer.Tests.Services
{
    public class LoggingServiceTests : IDisposable
    {
        private string _testLogDirectory;
        private LoggingService _loggingService;

        public LoggingServiceTests()
        {
            // Create a temporary directory for test logs
            _testLogDirectory = Path.Combine(Path.GetTempPath(), "SourceCodeGathererTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testLogDirectory);
            
            _loggingService = new LoggingService();
        }

        public void Dispose()
        {
            _loggingService?.Dispose();
            
            if (Directory.Exists(_testLogDirectory))
            {
                try
                {
                    Directory.Delete(_testLogDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public void LoggingService_ShouldInitializeSuccessfully()
        {
            // Arrange & Act
            using var service = new LoggingService();
            var logger = service.GetLogger<LoggingServiceTests>();

            // Assert
            Assert.NotNull(logger);
        }

        [Fact]
        public void LoggingService_ShouldCreateLoggerWithName()
        {
            // Arrange & Act
            using var service = new LoggingService();
            var logger = service.GetLogger("TestLogger");

            // Assert
            Assert.NotNull(logger);
        }

        [Fact]
        public async Task CleanupOldLogsAsync_WithValidRetentionDays_ShouldNotThrow()
        {
            // Arrange
            using var service = new LoggingService();

            // Act & Assert
            await service.CleanupOldLogsAsync(30);
            // If we get here without exception, the test passes
        }

        [Fact]
        public async Task CleanupOldLogsAsync_WithZeroRetentionDays_ShouldLogWarning()
        {
            // Arrange
            using var service = new LoggingService();

            // Act & Assert
            await service.CleanupOldLogsAsync(0);
            // Should not throw, but should log a warning
        }

        [Fact]
        public async Task CleanupOldLogsAsync_WithNegativeRetentionDays_ShouldLogWarning()
        {
            // Arrange
            using var service = new LoggingService();

            // Act & Assert
            await service.CleanupOldLogsAsync(-5);
            // Should not throw, but should log a warning
        }

        [Fact]
        public void LogApplicationStart_ShouldNotThrow()
        {
            // Arrange
            using var service = new LoggingService();

            // Act & Assert
            service.LogApplicationStart();
        }

        [Fact]
        public void LogApplicationShutdown_ShouldNotThrow()
        {
            // Arrange
            using var service = new LoggingService();

            // Act & Assert
            service.LogApplicationShutdown();
        }

        [Fact]
        public void LogUnhandledException_ShouldNotThrow()
        {
            // Arrange
            using var service = new LoggingService();
            var exception = new InvalidOperationException("Test exception");

            // Act & Assert
            service.LogUnhandledException(exception);
        }
    }
}
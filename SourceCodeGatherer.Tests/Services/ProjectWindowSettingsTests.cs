using System;
using System.IO;
using System.Threading.Tasks;
using SourceCodeGatherer.Models;
using SourceCodeGatherer.Services;
using Xunit;

namespace SourceCodeGatherer.Tests.Services
{
    /// <summary>
    /// Tests for project-specific window settings functionality.
    /// </summary>
    public class ProjectWindowSettingsTests
    {
        [Fact]
        public async Task GetProjectWindowSettingsAsync_WithNewProject_ReturnsValidSettings()
        {
            // Arrange
            var settingsService = new SettingsService();
            var projectPath = @"C:\TestProject_" + Guid.NewGuid().ToString(); // Use unique path

            // Act
            var settings = await settingsService.GetProjectWindowSettingsAsync(projectPath);

            // Assert
            Assert.NotNull(settings);
            Assert.True(settings.WindowWidth > 0);
            Assert.True(settings.WindowHeight > 0);
            Assert.NotNull(settings.WindowState);
            Assert.NotNull(settings.SelectedExtensions);
            Assert.True(settings.MaxFileSizeKB > 0);
        }

        [Fact]
        public async Task SaveAndGetProjectWindowSettingsAsync_WithValidSettings_PersistsCorrectly()
        {
            // Arrange
            var settingsService = new SettingsService();
            var projectPath = @"C:\TestProject_" + Guid.NewGuid().ToString();
            var windowSettings = new ProjectWindowSettings
            {
                WindowWidth = 1024,
                WindowHeight = 768,
                WindowLeft = 100,
                WindowTop = 50,
                WindowState = "Maximized",
                SelectedExtensions = new System.Collections.Generic.List<string> { ".cs", ".js", ".py" },
                MaxFileSizeKB = 5120,
                LastOutputPath = @"C:\Output\test.txt"
            };

            // Act
            await settingsService.SaveProjectWindowSettingsAsync(projectPath, windowSettings);
            var retrievedSettings = await settingsService.GetProjectWindowSettingsAsync(projectPath);

            // Assert
            Assert.NotNull(retrievedSettings);
            Assert.Equal(1024, retrievedSettings.WindowWidth);
            Assert.Equal(768, retrievedSettings.WindowHeight);
            Assert.Equal(100, retrievedSettings.WindowLeft);
            Assert.Equal(50, retrievedSettings.WindowTop);
            Assert.Equal("Maximized", retrievedSettings.WindowState);
            Assert.Equal(3, retrievedSettings.SelectedExtensions.Count);
            Assert.Contains(".cs", retrievedSettings.SelectedExtensions);
            Assert.Contains(".js", retrievedSettings.SelectedExtensions);
            Assert.Contains(".py", retrievedSettings.SelectedExtensions);
            Assert.Equal(5120, retrievedSettings.MaxFileSizeKB);
            Assert.Equal(@"C:\Output\test.txt", retrievedSettings.LastOutputPath);
        }

        [Fact]
        public async Task SaveProjectWindowSettingsAsync_WithMultipleProjects_StoresIndependently()
        {
            // Arrange
            var settingsService = new SettingsService();
            var project1Path = @"C:\Project1_" + Guid.NewGuid().ToString();
            var project2Path = @"C:\Project2_" + Guid.NewGuid().ToString();
            
            var settings1 = new ProjectWindowSettings
            {
                WindowWidth = 800,
                WindowHeight = 600,
                MaxFileSizeKB = 2048,
                SelectedExtensions = new System.Collections.Generic.List<string> { ".cs" }
            };
            
            var settings2 = new ProjectWindowSettings
            {
                WindowWidth = 1200,
                WindowHeight = 900,
                MaxFileSizeKB = 8192,
                SelectedExtensions = new System.Collections.Generic.List<string> { ".js", ".ts" }
            };

            // Act
            await settingsService.SaveProjectWindowSettingsAsync(project1Path, settings1);
            await settingsService.SaveProjectWindowSettingsAsync(project2Path, settings2);
            
            var retrieved1 = await settingsService.GetProjectWindowSettingsAsync(project1Path);
            var retrieved2 = await settingsService.GetProjectWindowSettingsAsync(project2Path);

            // Assert
            Assert.Equal(800, retrieved1.WindowWidth);
            Assert.Equal(2048, retrieved1.MaxFileSizeKB);
            Assert.Single(retrieved1.SelectedExtensions);
            Assert.Contains(".cs", retrieved1.SelectedExtensions);

            Assert.Equal(1200, retrieved2.WindowWidth);
            Assert.Equal(8192, retrieved2.MaxFileSizeKB);
            Assert.Equal(2, retrieved2.SelectedExtensions.Count);
            Assert.Contains(".js", retrieved2.SelectedExtensions);
            Assert.Contains(".ts", retrieved2.SelectedExtensions);
        }

        [Fact]
        public async Task GetProjectWindowSettingsAsync_WithEmptyPath_ReturnsDefaultSettings()
        {
            // Arrange
            var settingsService = new SettingsService();

            // Act
            var settings = await settingsService.GetProjectWindowSettingsAsync("");

            // Assert
            Assert.NotNull(settings);
            Assert.Equal(800, settings.WindowWidth);
            Assert.Equal(650, settings.WindowHeight);
        }

        [Fact]
        public async Task SaveProjectWindowSettingsAsync_WithEmptyPath_DoesNotThrow()
        {
            // Arrange
            var settingsService = new SettingsService();
            var windowSettings = new ProjectWindowSettings();

            // Act & Assert
            await settingsService.SaveProjectWindowSettingsAsync("", windowSettings);
            // Should not throw
        }
    }
}
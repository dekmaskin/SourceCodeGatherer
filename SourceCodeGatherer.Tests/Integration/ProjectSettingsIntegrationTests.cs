using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SourceCodeGatherer.Models;
using SourceCodeGatherer.Services;
using SourceCodeGatherer.ViewModels;
using Xunit;

namespace SourceCodeGatherer.Tests.Integration
{
    /// <summary>
    /// Integration tests for project-specific settings functionality.
    /// </summary>
    public class ProjectSettingsIntegrationTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly SettingsService _settingsService;

        public ProjectSettingsIntegrationTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "SourceCodeGathererIntegrationTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            _settingsService = new SettingsService();
        }

        [Fact]
        public async Task ProjectSettings_SaveAndRestore_WorksCorrectly()
        {
            // Arrange
            var projectPath = Path.Combine(_tempDirectory, "TestProject");
            Directory.CreateDirectory(projectPath);
            
            // Create some test files
            await File.WriteAllTextAsync(Path.Combine(projectPath, "test.cs"), "// C# file");
            await File.WriteAllTextAsync(Path.Combine(projectPath, "test.js"), "// JavaScript file");
            await File.WriteAllTextAsync(Path.Combine(projectPath, "test.py"), "# Python file");

            var originalSettings = new ProjectWindowSettings
            {
                ProjectPath = projectPath,
                WindowWidth = 1024,
                WindowHeight = 768,
                WindowLeft = 100,
                WindowTop = 50,
                WindowState = "Maximized",
                SelectedExtensions = new List<string> { ".cs", ".js" },
                MaxFileSizeKB = 5120,
                LastOutputPath = Path.Combine(_tempDirectory, "output.txt")
            };

            // Act - Save settings
            await _settingsService.SaveProjectWindowSettingsAsync(projectPath, originalSettings);

            // Act - Retrieve settings
            var retrievedSettings = await _settingsService.GetProjectWindowSettingsAsync(projectPath);

            // Assert
            Assert.NotNull(retrievedSettings);
            Assert.Equal(1024, retrievedSettings.WindowWidth);
            Assert.Equal(768, retrievedSettings.WindowHeight);
            Assert.Equal(100, retrievedSettings.WindowLeft);
            Assert.Equal(50, retrievedSettings.WindowTop);
            Assert.Equal("Maximized", retrievedSettings.WindowState);
            Assert.Equal(2, retrievedSettings.SelectedExtensions.Count);
            Assert.Contains(".cs", retrievedSettings.SelectedExtensions);
            Assert.Contains(".js", retrievedSettings.SelectedExtensions);
            Assert.Equal(5120, retrievedSettings.MaxFileSizeKB);
            Assert.Equal(Path.Combine(_tempDirectory, "output.txt"), retrievedSettings.LastOutputPath);
        }

        [Fact]
        public async Task ProjectSettings_MultipleProjects_RemainIndependent()
        {
            // Arrange
            var project1Path = Path.Combine(_tempDirectory, "Project1");
            var project2Path = Path.Combine(_tempDirectory, "Project2");
            Directory.CreateDirectory(project1Path);
            Directory.CreateDirectory(project2Path);

            var settings1 = new ProjectWindowSettings
            {
                WindowWidth = 800,
                WindowHeight = 600,
                SelectedExtensions = new List<string> { ".cs" },
                MaxFileSizeKB = 2048
            };

            var settings2 = new ProjectWindowSettings
            {
                WindowWidth = 1200,
                WindowHeight = 900,
                SelectedExtensions = new List<string> { ".js", ".ts" },
                MaxFileSizeKB = 8192
            };

            // Act
            await _settingsService.SaveProjectWindowSettingsAsync(project1Path, settings1);
            await _settingsService.SaveProjectWindowSettingsAsync(project2Path, settings2);

            var retrieved1 = await _settingsService.GetProjectWindowSettingsAsync(project1Path);
            var retrieved2 = await _settingsService.GetProjectWindowSettingsAsync(project2Path);

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

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
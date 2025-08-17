using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SourceCodeGatherer.Models;
using SourceCodeGatherer.Services;
using SourceCodeGatherer.ViewModels;
using Xunit;

namespace SourceCodeGatherer.Tests.Integration
{
    /// <summary>
    /// Integration tests specifically for file extension restoration functionality.
    /// </summary>
    public class FileExtensionRestorationTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly SettingsService _settingsService;
        private readonly FileService _fileService;

        public FileExtensionRestorationTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "FileExtensionRestorationTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            _settingsService = new SettingsService();
            _fileService = new FileService();
        }

        [Fact]
        public async Task FileExtensionRestoration_WithManualSetup_WorksCorrectly()
        {
            // Arrange
            var projectPath = Path.Combine(_tempDirectory, "TestProject");
            Directory.CreateDirectory(projectPath);
            
            // Create test files with different extensions
            await File.WriteAllTextAsync(Path.Combine(projectPath, "test.cs"), "// C# file");
            await File.WriteAllTextAsync(Path.Combine(projectPath, "test.js"), "// JavaScript file");
            await File.WriteAllTextAsync(Path.Combine(projectPath, "test.py"), "# Python file");
            await File.WriteAllTextAsync(Path.Combine(projectPath, "test.txt"), "Text file");

            // Save project settings with specific extensions selected
            var projectSettings = new ProjectWindowSettings
            {
                ProjectPath = projectPath,
                SelectedExtensions = new List<string> { ".cs", ".js" },
                MaxFileSizeKB = 5120
            };
            await _settingsService.SaveProjectWindowSettingsAsync(projectPath, projectSettings);

            // Create MainViewModel
            var viewModel = new MainViewModel(_fileService, _settingsService);
            
            // Manually populate file extensions (simulating what ScanDirectoryAsync would do)
            viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".cs", IsChecked = false });
            viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".js", IsChecked = false });
            viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".py", IsChecked = false });
            viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".txt", IsChecked = false });
            
            // Simulate the restoration process
            var retrievedSettings = await _settingsService.GetProjectWindowSettingsAsync(projectPath);
            viewModel.RestoreProjectSettings(retrievedSettings);
            
            // Verify that the correct extensions are selected
            var selectedExtensions = viewModel.FileExtensions.Where(x => x.IsChecked).Select(x => x.Extension).ToList();
            
            Assert.Contains(".cs", selectedExtensions);
            Assert.Contains(".js", selectedExtensions);
            Assert.DoesNotContain(".py", selectedExtensions);
            Assert.DoesNotContain(".txt", selectedExtensions);
            Assert.Equal(2, selectedExtensions.Count);
        }

        [Fact]
        public async Task FileExtensionRestoration_WithNoSavedSettings_DoesNotCrash()
        {
            // Arrange
            var projectPath = Path.Combine(_tempDirectory, "NewProject");
            Directory.CreateDirectory(projectPath);

            // Create MainViewModel
            var viewModel = new MainViewModel(_fileService, _settingsService);
            
            // Manually populate file extensions
            viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".cs", IsChecked = false });
            viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".js", IsChecked = false });
            
            // Try to restore settings for a project with no saved settings
            var retrievedSettings = await _settingsService.GetProjectWindowSettingsAsync(projectPath);
            
            // This should not crash
            viewModel.RestoreProjectSettings(retrievedSettings);
            
            // All extensions should remain unchecked since there are no saved settings
            var selectedExtensions = viewModel.FileExtensions.Where(x => x.IsChecked).ToList();
            Assert.Empty(selectedExtensions);
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
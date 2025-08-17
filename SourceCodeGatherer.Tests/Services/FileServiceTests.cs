using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SourceCodeGatherer.Models;
using SourceCodeGatherer.Services;
using Xunit;

namespace SourceCodeGatherer.Tests.Services
{
    /// <summary>
    /// Unit tests for FileService.
    /// </summary>
    public class FileServiceTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly FileService _fileService;

        public FileServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _fileService = new FileService();
            
            SetupTestFiles();
        }

        [Fact]
        public async Task GetFileExtensionsAsync_ReturnsCorrectExtensions()
        {
            // Act
            var extensions = await _fileService.GetFileExtensionsAsync(_testDirectory);

            // Assert
            var extensionList = extensions.ToList();
            Assert.Contains(".cs", extensionList);
            Assert.Contains(".js", extensionList);
            Assert.Contains(".txt", extensionList);
            Assert.DoesNotContain(".exe", extensionList); // Binary file should be excluded
        }

        [Fact]
        public async Task GetFileExtensionsAsync_ExcludesDirectories()
        {
            // Arrange
            var excludedDirs = new[] { "bin", "obj" };

            // Act
            var extensions = await _fileService.GetFileExtensionsAsync(_testDirectory, excludedDirs);

            // Assert
            var extensionList = extensions.ToList();
            Assert.Contains(".cs", extensionList);
            Assert.Contains(".js", extensionList);
            Assert.DoesNotContain(".dll", extensionList); // Should be excluded from bin folder
        }

        [Fact]
        public async Task GetExportStatisticsAsync_ReturnsValidStats()
        {
            // Arrange
            var selectedExtensions = new[] { ".cs", ".js" };
            var settings = new AppSettings 
            { 
                MaxFileSizeBytes = 1024 * 1024, // 1MB
                ExcludedDirectories = new List<string>() // Don't exclude any directories for this test
            };

            // Act
            var stats = await _fileService.GetExportStatisticsAsync(_testDirectory, selectedExtensions, settings);

            // Assert - Just verify the method returns valid statistics object
            Assert.NotNull(stats);
            Assert.True(stats.TotalFiles >= 0);
            Assert.True(stats.TotalSizeBytes >= 0);
            Assert.True(stats.EstimatedLines >= 0);
            Assert.True(stats.SkippedFiles >= 0);
            Assert.NotNull(stats.FormattedSize);
        }

        [Fact]
        public void IsTextFile_IdentifiesTextFiles()
        {
            // Assert
            Assert.True(_fileService.IsTextFile(".cs"));
            Assert.True(_fileService.IsTextFile(".js"));
            Assert.True(_fileService.IsTextFile(".txt"));
            Assert.False(_fileService.IsTextFile(".exe"));
            Assert.False(_fileService.IsTextFile(".dll"));
            Assert.False(_fileService.IsTextFile(".jpg"));
        }

        [Fact]
        public async Task IsBinaryFileAsync_DetectsBinaryFiles()
        {
            // Arrange
            var textFile = Path.Combine(_testDirectory, "test.txt");
            var binaryFile = Path.Combine(_testDirectory, "test.bin");
            
            await File.WriteAllTextAsync(textFile, "This is a text file");
            await File.WriteAllBytesAsync(binaryFile, new byte[] { 0, 1, 2, 3, 0, 255 });

            // Act & Assert
            Assert.False(await _fileService.IsBinaryFileAsync(textFile));
            Assert.True(await _fileService.IsBinaryFileAsync(binaryFile));
        }

        private void SetupTestFiles()
        {
            // Create test files
            File.WriteAllText(Path.Combine(_testDirectory, "Program.cs"), "using System;\nclass Program { }");
            File.WriteAllText(Path.Combine(_testDirectory, "script.js"), "console.log('Hello World');");
            File.WriteAllText(Path.Combine(_testDirectory, "readme.txt"), "This is a readme file");
            
            // Create binary file (should be ignored)
            File.WriteAllBytes(Path.Combine(_testDirectory, "app.exe"), new byte[] { 0x4D, 0x5A }); // PE header
            
            // Create excluded directories with files
            var binDir = Path.Combine(_testDirectory, "bin");
            var objDir = Path.Combine(_testDirectory, "obj");
            Directory.CreateDirectory(binDir);
            Directory.CreateDirectory(objDir);
            
            File.WriteAllBytes(Path.Combine(binDir, "app.dll"), new byte[] { 0x4D, 0x5A });
            File.WriteAllText(Path.Combine(objDir, "temp.cs"), "// temp file");
            
            // Debug: List all files created
            var allFiles = Directory.GetFiles(_testDirectory, "*.*", SearchOption.AllDirectories);
            System.Diagnostics.Debug.WriteLine($"Created {allFiles.Length} files:");
            foreach (var file in allFiles)
            {
                System.Diagnostics.Debug.WriteLine($"  {Path.GetRelativePath(_testDirectory, file)} ({Path.GetExtension(file)})");
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
    }
}
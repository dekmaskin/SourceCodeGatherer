using System;
using System.IO;
using Xunit;
using SourceCodeGatherer.Models;

namespace SourceCodeGatherer.Tests.Models
{
    public class FileItemTests
    {
        [Fact]
        public void FileItem_FromPath_CreatesCorrectItem()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var rootPath = Path.GetTempPath();
            File.WriteAllText(tempFile, "test content");

            try
            {
                // Act
                var fileItem = FileItem.FromPath(tempFile, rootPath);

                // Assert
                Assert.Equal(tempFile, fileItem.FullPath);
                Assert.Equal(Path.GetRelativePath(rootPath, tempFile), fileItem.RelativePath);
                Assert.Equal(Path.GetFileName(tempFile), fileItem.FileName);
                Assert.Equal(Path.GetExtension(tempFile), fileItem.Extension);
                Assert.True(fileItem.SizeBytes > 0);
                Assert.True(fileItem.IsIncluded);
                Assert.True(fileItem.IncludeContent);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void FileItem_IsIncluded_WhenSetToFalse_SetsIncludeContentToFalse()
        {
            // Arrange
            var fileItem = new FileItem
            {
                IsIncluded = true,
                IncludeContent = true
            };

            // Act
            fileItem.IsIncluded = false;

            // Assert
            Assert.False(fileItem.IsIncluded);
            Assert.False(fileItem.IncludeContent);
        }

        [Fact]
        public void FileItem_IncludeContent_WhenSetToTrue_SetsIsIncludedToTrue()
        {
            // Arrange
            var fileItem = new FileItem
            {
                IsIncluded = false,
                IncludeContent = false
            };

            // Act
            fileItem.IncludeContent = true;

            // Assert
            Assert.True(fileItem.IsIncluded);
            Assert.True(fileItem.IncludeContent);
        }

        [Theory]
        [InlineData(".cs", "C#/VB.NET")]
        [InlineData(".js", "JavaScript/TypeScript")]
        [InlineData(".py", "Python")]
        [InlineData(".java", "Java")]
        [InlineData(".html", "HTML")]
        [InlineData(".css", "CSS")]
        [InlineData(".xml", "XML")]
        [InlineData(".json", "JSON")]
        [InlineData(".md", "Markdown")]
        [InlineData(".txt", "Text")]
        [InlineData(".unknown", "Other")]
        public void FileItem_FileType_ReturnsCorrectCategory(string extension, string expectedType)
        {
            // Arrange
            var fileItem = new FileItem { Extension = extension };

            // Act
            var fileType = fileItem.FileType;

            // Assert
            Assert.Equal(expectedType, fileType);
        }

        [Theory]
        [InlineData(100, "100 B")]
        [InlineData(1536, "1,5 KB")]
        [InlineData(2097152, "2,0 MB")]
        public void FileItem_FormattedSize_ReturnsCorrectFormat(long sizeBytes, string expectedFormat)
        {
            // Arrange
            var fileItem = new FileItem { SizeBytes = sizeBytes };

            // Act
            var formattedSize = fileItem.FormattedSize;

            // Assert
            Assert.Equal(expectedFormat, formattedSize);
        }

        [Fact]
        public void FileItem_PropertyChanged_IsRaisedForIsIncluded()
        {
            // Arrange
            var fileItem = new FileItem();
            var propertyChangedRaised = false;
            fileItem.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(FileItem.IsIncluded))
                    propertyChangedRaised = true;
            };

            // Act
            fileItem.IsIncluded = false;

            // Assert
            Assert.True(propertyChangedRaised);
        }

        [Fact]
        public void FileItem_PropertyChanged_IsRaisedForIncludeContent()
        {
            // Arrange
            var fileItem = new FileItem();
            var propertyChangedRaised = false;
            fileItem.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(FileItem.IncludeContent))
                    propertyChangedRaised = true;
            };

            // Act
            fileItem.IncludeContent = false;

            // Assert
            Assert.True(propertyChangedRaised);
        }

        [Theory]
        [InlineData(@"src\main\Program.cs", "src\\main")]
        [InlineData(@"Program.cs", ".")]
        [InlineData(@"folder\subfolder\file.txt", "folder\\subfolder")]
        [InlineData(@"test.js", ".")]
        public void FileItem_RelativeDirectory_ReturnsCorrectDirectory(string relativePath, string expectedDirectory)
        {
            // Arrange
            var fileItem = new FileItem { RelativePath = relativePath };

            // Act
            var relativeDirectory = fileItem.RelativeDirectory;

            // Assert
            Assert.Equal(expectedDirectory, relativeDirectory);
        }
    }
}
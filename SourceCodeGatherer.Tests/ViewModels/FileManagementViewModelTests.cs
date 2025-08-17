using System.Collections.Generic;
using System.Linq;
using Xunit;
using SourceCodeGatherer.Models;
using SourceCodeGatherer.ViewModels;

namespace SourceCodeGatherer.Tests.ViewModels
{
    public class FileManagementViewModelTests
    {
        [Fact]
        public void FileManagementViewModel_SearchText_FiltersFiles()
        {
            // Arrange
            var files = new List<FileItem>
            {
                new FileItem { FileName = "Program.cs", RelativePath = @"src\Program.cs", Extension = ".cs" },
                new FileItem { FileName = "Test.js", RelativePath = @"tests\Test.js", Extension = ".js" },
                new FileItem { FileName = "Style.css", RelativePath = @"assets\Style.css", Extension = ".css" }
            };
            var viewModel = new FileManagementViewModel(files);

            // Act
            viewModel.SearchText = "Program";

            // Assert
            var filteredFiles = viewModel.FilteredFiles.Cast<FileItem>().ToList();
            Assert.Single(filteredFiles);
            Assert.Equal("Program.cs", filteredFiles[0].FileName);
        }

        [Fact]
        public void FileManagementViewModel_SearchText_SearchesInDirectory()
        {
            // Arrange
            var files = new List<FileItem>
            {
                new FileItem { FileName = "Program.cs", RelativePath = @"src\main\Program.cs", Extension = ".cs" },
                new FileItem { FileName = "Test.js", RelativePath = @"tests\unit\Test.js", Extension = ".js" },
                new FileItem { FileName = "Style.css", RelativePath = @"assets\css\Style.css", Extension = ".css" }
            };
            var viewModel = new FileManagementViewModel(files);

            // Act
            viewModel.SearchText = "main";

            // Assert
            var filteredFiles = viewModel.FilteredFiles.Cast<FileItem>().ToList();
            Assert.Single(filteredFiles);
            Assert.Equal("Program.cs", filteredFiles[0].FileName);
        }

        [Fact]
        public void FileManagementViewModel_ClearSearch_ShowsAllFiles()
        {
            // Arrange
            var files = new List<FileItem>
            {
                new FileItem { FileName = "Program.cs", RelativePath = @"src\Program.cs", Extension = ".cs" },
                new FileItem { FileName = "Test.js", RelativePath = @"tests\Test.js", Extension = ".js" }
            };
            var viewModel = new FileManagementViewModel(files);
            viewModel.SearchText = "Program";

            // Act
            viewModel.ClearSearchCommand.Execute(null);

            // Assert
            var filteredFiles = viewModel.FilteredFiles.Cast<FileItem>().ToList();
            Assert.Equal(2, filteredFiles.Count);
        }

        [Fact]
        public void FileManagementViewModel_SelectAll_IncludesAllFiles()
        {
            // Arrange
            var files = new List<FileItem>
            {
                new FileItem { FileName = "Program.cs", Extension = ".cs", IsIncluded = false },
                new FileItem { FileName = "Test.js", Extension = ".js", IsIncluded = false }
            };
            var viewModel = new FileManagementViewModel(files);

            // Act
            viewModel.SelectAllCommand.Execute(null);

            // Assert
            Assert.All(viewModel.AllFiles, file => Assert.True(file.IsIncluded));
        }

        [Fact]
        public void FileManagementViewModel_SelectNone_ExcludesAllFiles()
        {
            // Arrange
            var files = new List<FileItem>
            {
                new FileItem { FileName = "Program.cs", Extension = ".cs", IsIncluded = true },
                new FileItem { FileName = "Test.js", Extension = ".js", IsIncluded = true }
            };
            var viewModel = new FileManagementViewModel(files);

            // Act
            viewModel.SelectNoneCommand.Execute(null);

            // Assert
            Assert.All(viewModel.AllFiles, file => Assert.False(file.IsIncluded));
        }

        [Fact]
        public void FileManagementViewModel_Counts_UpdateCorrectly()
        {
            // Arrange
            var files = new List<FileItem>
            {
                new FileItem { FileName = "Program.cs", Extension = ".cs", IsIncluded = true, IncludeContent = true },
                new FileItem { FileName = "Test.js", Extension = ".js", IsIncluded = true, IncludeContent = false },
                new FileItem { FileName = "Style.css", Extension = ".css", IsIncluded = false, IncludeContent = false }
            };
            var viewModel = new FileManagementViewModel(files);

            // Assert
            Assert.Equal(2, viewModel.IncludedFilesCount);
            Assert.Equal(1, viewModel.ContentIncludedCount);
        }
    }
}
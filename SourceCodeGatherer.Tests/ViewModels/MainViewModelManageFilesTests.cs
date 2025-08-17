using System.Linq;
using Moq;
using Xunit;
using SourceCodeGatherer.Models;
using SourceCodeGatherer.Services;
using SourceCodeGatherer.ViewModels;

namespace SourceCodeGatherer.Tests.ViewModels
{
    public class MainViewModelManageFilesTests
    {
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<ISettingsService> _mockSettingsService;
        private readonly MainViewModel _viewModel;

        public MainViewModelManageFilesTests()
        {
            _mockFileService = new Mock<IFileService>();
            _mockSettingsService = new Mock<ISettingsService>();
            _viewModel = new MainViewModel(_mockFileService.Object, _mockSettingsService.Object);
        }

        [Fact]
        public void CanManageFiles_WhenRootPathIsEmpty_ReturnsFalse()
        {
            // Arrange
            _viewModel.RootPath = "";

            // Act
            var canManage = _viewModel.CanManageFiles;

            // Assert
            Assert.False(canManage);
        }

        [Fact]
        public void CanManageFiles_WhenNoFileExtensionsChecked_ReturnsFalse()
        {
            // Arrange
            _viewModel.RootPath = @"C:\TestPath";
            _viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".cs", IsChecked = false });

            // Act
            var canManage = _viewModel.CanManageFiles;

            // Assert
            Assert.False(canManage);
        }

        [Fact]
        public void CanManageFiles_WhenIsProcessing_ReturnsFalse()
        {
            // Arrange
            _viewModel.RootPath = @"C:\TestPath";
            _viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".cs", IsChecked = true });
            _viewModel.IsProcessing = true;

            // Act
            var canManage = _viewModel.CanManageFiles;

            // Assert
            Assert.False(canManage);
        }

        [Fact]
        public void CanManageFiles_WhenAllConditionsMet_ReturnsTrue()
        {
            // Arrange
            _viewModel.RootPath = @"C:\TestPath";
            _viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".cs", IsChecked = true });
            _viewModel.IsProcessing = false;

            // Act
            var canManage = _viewModel.CanManageFiles;

            // Assert
            Assert.True(canManage);
        }

        [Fact]
        public void CanManageFiles_WhenFileExtensionCheckedStateChanges_UpdatesCorrectly()
        {
            // Arrange
            _viewModel.RootPath = @"C:\TestPath";
            var fileExtension = new FileExtensionItem { Extension = ".cs", IsChecked = false };
            _viewModel.FileExtensions.Add(fileExtension);

            // Act & Assert - Initially false
            Assert.False(_viewModel.CanManageFiles);

            // Act & Assert - After checking extension
            fileExtension.IsChecked = true;
            Assert.True(_viewModel.CanManageFiles);

            // Act & Assert - After unchecking extension
            fileExtension.IsChecked = false;
            Assert.False(_viewModel.CanManageFiles);
        }
    }
}
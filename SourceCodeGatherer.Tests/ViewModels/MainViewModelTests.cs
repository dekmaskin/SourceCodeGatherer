using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using SourceCodeGatherer.Models;
using SourceCodeGatherer.Services;
using SourceCodeGatherer.ViewModels;
using Xunit;

namespace SourceCodeGatherer.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for MainViewModel.
    /// </summary>
    public class MainViewModelTests
    {
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<ISettingsService> _mockSettingsService;
        private readonly MainViewModel _viewModel;

        public MainViewModelTests()
        {
            _mockFileService = new Mock<IFileService>();
            _mockSettingsService = new Mock<ISettingsService>();
            
            _mockSettingsService.Setup(s => s.LoadSettingsAsync())
                .ReturnsAsync(new AppSettings());
            
            _viewModel = new MainViewModel(_mockFileService.Object, _mockSettingsService.Object);
        }

        [Fact]
        public void CanExport_ReturnsFalse_WhenNoRootPath()
        {
            // Arrange
            _viewModel.RootPath = string.Empty;
            _viewModel.OutputPath = "test.txt";

            // Act & Assert
            Assert.False(_viewModel.CanExport);
        }

        [Fact]
        public void CanExport_ReturnsFalse_WhenNoOutputPath()
        {
            // Arrange
            _viewModel.RootPath = @"C:\Test";
            _viewModel.OutputPath = string.Empty;

            // Act & Assert
            Assert.False(_viewModel.CanExport);
        }

        [Fact]
        public void CanExport_ReturnsFalse_WhenNoExtensionsSelected()
        {
            // Arrange
            _viewModel.RootPath = @"C:\Test";
            _viewModel.OutputPath = "test.txt";
            _viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".cs", IsChecked = false });

            // Act & Assert
            Assert.False(_viewModel.CanExport);
        }

        [Fact]
        public void CanExport_ReturnsTrue_WhenAllConditionsMet()
        {
            // Arrange
            _viewModel.RootPath = @"C:\Test";
            _viewModel.OutputPath = "test.txt";
            _viewModel.FileExtensions.Add(new FileExtensionItem { Extension = ".cs", IsChecked = true });

            // Act & Assert
            Assert.True(_viewModel.CanExport);
        }

        [Fact]
        public void IsTextFile_WorksCorrectly()
        {
            // Arrange
            var fileService = new FileService();

            // Act & Assert
            Assert.True(fileService.IsTextFile(".cs"));
            Assert.True(fileService.IsTextFile(".js"));
            Assert.False(fileService.IsTextFile(".exe"));
        }

        [Fact]
        public async Task GetFileExtensionsAsync_CallsService()
        {
            // Arrange
            var expectedExtensions = new[] { ".cs", ".js" };
            _mockFileService.Setup(s => s.GetFileExtensionsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(expectedExtensions);

            // Act
            var result = await _mockFileService.Object.GetFileExtensionsAsync(@"C:\Test");

            // Assert
            _mockFileService.Verify(s => s.GetFileExtensionsAsync(@"C:\Test", It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()), Times.Once);
            Assert.Equal(expectedExtensions, result);
        }
    }
}
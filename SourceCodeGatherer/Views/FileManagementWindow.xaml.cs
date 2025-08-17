using System.Windows;
using SourceCodeGatherer.ViewModels;

namespace SourceCodeGatherer.Views
{
    /// <summary>
    /// Interaction logic for FileManagementWindow.xaml
    /// </summary>
    public partial class FileManagementWindow : Window
    {
        public FileManagementWindow(FileManagementViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // Handle dialog result from view model
            viewModel.CloseRequested += (sender, result) =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}
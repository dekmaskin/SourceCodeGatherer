using System;
using System.IO;
using System.Linq;
using System.Windows;
using SourceCodeGatherer.ViewModels;
using SourceCodeGatherer.Services;
using SourceCodeGatherer.Models;

namespace SourceCodeGatherer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ISettingsService _settingsService;
        private MainViewModel _viewModel;
        private string _currentProjectPath;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            _viewModel = new MainViewModel(new FileService(), _settingsService);
            DataContext = _viewModel;
            
            // Subscribe to property changes to track current project
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.DirectoryScanCompleted += ViewModel_DirectoryScanCompleted;
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        /// <summary>
        /// Handles the click event for the close button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the drag over event for folder drag and drop.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length == 1 && Directory.Exists(files[0]))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handles the drop event for folder drag and drop.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length == 1 && Directory.Exists(files[0]))
                {
                    if (DataContext is MainViewModel viewModel)
                    {
                        viewModel.RootPath = files[0];
                    }
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handles the recent paths combo box selection changed event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void RecentPathsComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is string selectedPath)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.RootPath = selectedPath;
                }
                // Clear selection to allow selecting the same item again
                RecentPathsComboBox.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Handles the window loaded event to restore project-specific settings.
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // If we have a current project path, restore its window settings
            if (!string.IsNullOrWhiteSpace(_currentProjectPath))
            {
                await RestoreWindowSettingsAsync(_currentProjectPath);
            }
        }

        /// <summary>
        /// Handles the window closing event to save project-specific settings.
        /// </summary>
        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save current window settings for the current project
            if (!string.IsNullOrWhiteSpace(_currentProjectPath))
            {
                await SaveWindowSettingsAsync(_currentProjectPath);
            }
        }

        /// <summary>
        /// Handles property changes in the view model to track project changes.
        /// </summary>
        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.RootPath))
            {
                var newProjectPath = _viewModel.RootPath;
                
                // Save settings for the previous project
                if (!string.IsNullOrWhiteSpace(_currentProjectPath) && 
                    !string.Equals(_currentProjectPath, newProjectPath, StringComparison.OrdinalIgnoreCase))
                {
                    await SaveWindowSettingsAsync(_currentProjectPath);
                }
                
                // Update current project
                _currentProjectPath = newProjectPath;
                
                // Restore window settings (size, position, etc.) immediately
                // File extension settings will be restored after directory scan completes
                if (!string.IsNullOrWhiteSpace(_currentProjectPath))
                {
                    await RestoreWindowSettingsAsync(_currentProjectPath);
                }
            }
        }

        /// <summary>
        /// Handles the directory scan completed event to restore project-specific file extension settings.
        /// </summary>
        private async void ViewModel_DirectoryScanCompleted(object sender, EventArgs e)
        {
            // Restore project-specific settings after directory scan completes
            if (!string.IsNullOrWhiteSpace(_currentProjectPath))
            {
                try
                {
                    var settings = await _settingsService.GetProjectWindowSettingsAsync(_currentProjectPath);
                    
                    // Ensure we're on the UI thread when modifying the view model
                    Dispatcher.Invoke(() =>
                    {
                        _viewModel.RestoreProjectSettings(settings);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ViewModel_DirectoryScanCompleted: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Restores window settings for the specified project.
        /// </summary>
        private async System.Threading.Tasks.Task RestoreWindowSettingsAsync(string projectPath)
        {
            try
            {
                var settings = await _settingsService.GetProjectWindowSettingsAsync(projectPath);
                
                // Restore window size and position
                if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
                {
                    Width = settings.WindowWidth;
                    Height = settings.WindowHeight;
                }
                
                if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
                {
                    // Ensure the window is visible on screen
                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    var screenHeight = SystemParameters.PrimaryScreenHeight;
                    
                    if (settings.WindowLeft >= 0 && settings.WindowLeft < screenWidth - 100 &&
                        settings.WindowTop >= 0 && settings.WindowTop < screenHeight - 100)
                    {
                        Left = settings.WindowLeft;
                        Top = settings.WindowTop;
                        WindowStartupLocation = WindowStartupLocation.Manual;
                    }
                }
                
                // Restore window state
                if (Enum.TryParse<WindowState>(settings.WindowState, out var windowState))
                {
                    WindowState = windowState;
                }
                
                // Restore immediate settings (file size, output path)
                _viewModel.RestoreImmediateProjectSettings(settings);
            }
            catch
            {
                // Silently fail - settings are not critical
            }
        }

        /// <summary>
        /// Saves window settings for the specified project.
        /// </summary>
        private async System.Threading.Tasks.Task SaveWindowSettingsAsync(string projectPath)
        {
            try
            {
                var settings = new ProjectWindowSettings
                {
                    ProjectPath = projectPath,
                    WindowWidth = ActualWidth,
                    WindowHeight = ActualHeight,
                    WindowLeft = Left,
                    WindowTop = Top,
                    WindowState = WindowState.ToString(),
                    SelectedExtensions = _viewModel.GetSelectedExtensions(),
                    MaxFileSizeKB = _viewModel.MaxFileSizeKB,
                    LastOutputPath = _viewModel.OutputPath ?? string.Empty
                };
                
                await _settingsService.SaveProjectWindowSettingsAsync(projectPath, settings);
            }
            catch
            {
                // Silently fail - settings are not critical
            }
        }
    }
}
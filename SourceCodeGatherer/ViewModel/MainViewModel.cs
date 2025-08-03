using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SourceCodeGatherer.Commands;
using SourceCodeGatherer.Models;
using SourceCodeGatherer.Services;

namespace SourceCodeGatherer.ViewModels
{
    /// <summary>
    /// ViewModel for the main window, implementing MVVM pattern for source code gathering.
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly IFileService _fileService;
        private string _rootPath;
        private string _outputPath;
        private bool _isProcessing;
        private string _statusMessage;
        private Visibility _progressVisibility = Visibility.Collapsed;
        private Visibility _fileTypesVisibility = Visibility.Collapsed;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel() : this(new FileService())
        {
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class with dependency injection.
        /// </summary>
        /// <param name="fileService">The file service.</param>
        public MainViewModel(IFileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));

            FileExtensions = new ObservableCollection<FileExtensionItem>();
            InitializeCommands();
            StatusMessage = "Select a root directory to begin.";
        }

        #region Properties

        /// <summary>
        /// Gets or sets the root path for scanning source files.
        /// </summary>
        public string RootPath
        {
            get => _rootPath;
            set
            {
                if (SetProperty(ref _rootPath, value))
                {
                    UpdateDefaultOutputPath();
                    OnPropertyChanged(nameof(CanExport));
                    CommandManager.InvalidateRequerySuggested();

                    // Automatically scan directory when path is set
                    if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
                    {
                        _ = ScanDirectoryAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the output file path.
        /// </summary>
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (SetProperty(ref _outputPath, value))
                {
                    OnPropertyChanged(nameof(CanExport));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the application is currently processing.
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    ProgressVisibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Gets or sets the status message displayed to the user.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the progress bar.
        /// </summary>
        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set => SetProperty(ref _progressVisibility, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the file types text.
        /// </summary>
        public Visibility FileTypesVisibility
        {
            get => _fileTypesVisibility;
            set => SetProperty(ref _fileTypesVisibility, value);
        }

        /// <summary>
        /// Gets whether the export operation can be executed.
        /// </summary>
        public bool CanExport
        {
            get
            {
                var canExport = !string.IsNullOrWhiteSpace(RootPath) &&
                               !string.IsNullOrWhiteSpace(OutputPath) &&
                               FileExtensions.Any(x => x.IsChecked);

                UpdateStatusMessage();
                return canExport;
            }
        }

        /// <summary>
        /// Gets the collection of file extensions found in the directory.
        /// </summary>
        public ObservableCollection<FileExtensionItem> FileExtensions { get; }

        #endregion

        #region Commands

        public ICommand BrowseRootCommand { get; private set; }
        public ICommand BrowseOutputCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }
        public ICommand ExportToClipboardCommand { get; private set; }

        #endregion

        #region Private Methods

        private void InitializeCommands()
        {
            BrowseRootCommand = new RelayCommand(ExecuteBrowseRoot);
            BrowseOutputCommand = new RelayCommand(ExecuteBrowseOutput);
            ExportCommand = new RelayCommand(ExecuteExport, CanExecuteExport);
            ExportToClipboardCommand = new RelayCommand(ExecuteExportToClipboard, CanExecuteExport);
        }

        private void UpdateStatusMessage()
        {
            if (!string.IsNullOrWhiteSpace(RootPath) && !string.IsNullOrWhiteSpace(OutputPath))
            {
                if (FileExtensions.Count == 0)
                {
                    StatusMessage = "Scanning directory for file types...";
                }
                else if (!FileExtensions.Any(x => x.IsChecked))
                {
                    StatusMessage = "Select at least one file type to export.";
                }
                else
                {
                    StatusMessage = "Ready to export.";
                }
            }
        }

        private void ExecuteBrowseRoot()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Root Directory";
                dialog.ShowNewFolderButton = false;

                if (!string.IsNullOrWhiteSpace(RootPath) && Directory.Exists(RootPath))
                {
                    dialog.SelectedPath = RootPath;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    RootPath = dialog.SelectedPath;
                }
            }
        }

        private void ExecuteBrowseOutput()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt",
                FileName = Path.GetFileName(OutputPath)
            };

            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(OutputPath);
            }

            if (dialog.ShowDialog() == true)
            {
                OutputPath = dialog.FileName;
            }
        }

        private async Task ScanDirectoryAsync()
        {
            IsProcessing = true;
            FileExtensions.Clear();
            FileTypesVisibility = Visibility.Collapsed;
            StatusMessage = "Scanning directory...";

            try
            {
                var extensions = await _fileService.GetFileExtensionsAsync(RootPath);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var ext in extensions)
                    {
                        var item = new FileExtensionItem { Extension = ext };
                        item.PropertyChanged += OnFileExtensionItemPropertyChanged;
                        FileExtensions.Add(item);
                    }

                    if (FileExtensions.Count > 0)
                    {
                        FileTypesVisibility = Visibility.Visible;
                        StatusMessage = $"Found {FileExtensions.Count} file types. Select the ones to include.";
                    }
                    else
                    {
                        StatusMessage = "No text files found in the selected directory.";
                    }

                    OnPropertyChanged(nameof(CanExport));
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowError($"Error scanning directory: {ex.Message}");
                    StatusMessage = "Error occurred during scanning.";
                });
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void OnFileExtensionItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileExtensionItem.IsChecked))
            {
                OnPropertyChanged(nameof(CanExport));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool CanExecuteExport()
        {
            return CanExport;
        }

        private async void ExecuteExport()
        {
            IsProcessing = true;
            StatusMessage = "Exporting files...";

            try
            {
                var selectedExtensions = FileExtensions
                    .Where(x => x.IsChecked)
                    .Select(x => x.Extension)
                    .ToList();

                await _fileService.ExportFilesAsync(RootPath, OutputPath, selectedExtensions);

                StatusMessage = "Export completed successfully!";
                ShowSuccess("Export completed successfully!");
            }
            catch (Exception ex)
            {
                ShowError($"Error during export: {ex.Message}");
                StatusMessage = "Error occurred during export.";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void ExecuteExportToClipboard()
        {
            IsProcessing = true;
            StatusMessage = "Exporting to clipboard...";

            try
            {
                var selectedExtensions = FileExtensions
                    .Where(x => x.IsChecked)
                    .Select(x => x.Extension)
                    .ToList();

                var content = await _fileService.ExportFilesToStringAsync(RootPath, selectedExtensions);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Clipboard.SetText(content);
                });

                StatusMessage = "Exported to clipboard successfully!";
                ShowSuccess("Content has been copied to clipboard!");
            }
            catch (Exception ex)
            {
                ShowError($"Error during clipboard export: {ex.Message}");
                StatusMessage = "Error occurred during clipboard export.";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void UpdateDefaultOutputPath()
        {
            if (!string.IsNullOrWhiteSpace(RootPath) && Directory.Exists(RootPath))
            {
                var rootDirName = new DirectoryInfo(RootPath).Name;
                var downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
                var date = DateTime.Now.ToString("yyyy-MM-dd");
                var time = DateTime.Now.ToString("HHmm");
                OutputPath = Path.Combine(downloadsPath, $"{rootDirName}_{date}_{time}.txt");
                OnPropertyChanged(nameof(CanExport));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowSuccess(string message)
        {
            MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
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
        private readonly ISettingsService _settingsService;
        private AppSettings _settings;
        private string _rootPath;
        private string _outputPath;
        private bool _isProcessing;
        private string _statusMessage;
        private int _progressValue;
        private string _progressText = string.Empty;
        private Visibility _progressVisibility = Visibility.Collapsed;
        private Visibility _fileTypesVisibility = Visibility.Collapsed;
        private Visibility _statisticsVisibility = Visibility.Collapsed;
        private ExportStatistics _exportStatistics;
        private List<FileItem> _managedFiles;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel() : this(new FileService(), new SettingsService())
        {
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class with dependency injection.
        /// </summary>
        /// <param name="fileService">The file service.</param>
        /// <param name="settingsService">The settings service.</param>
        public MainViewModel(IFileService fileService, ISettingsService settingsService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            FileExtensions = new ObservableCollection<FileExtensionItem>();
            RecentPaths = new ObservableCollection<string>();
            InitializeCommands();
            _ = LoadSettingsAsync();
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
                    OnPropertyChanged(nameof(CanManageFiles));
                    CommandManager.InvalidateRequerySuggested();

                    // Clear managed files when root path changes
                    _managedFiles = null;

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
                    OnPropertyChanged(nameof(CanManageFiles));
                    CommandManager.InvalidateRequerySuggested();
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
        /// Gets or sets the progress value (0-100).
        /// </summary>
        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        /// <summary>
        /// Gets or sets the progress text.
        /// </summary>
        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
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
        /// Gets or sets the visibility of the statistics panel.
        /// </summary>
        public Visibility StatisticsVisibility
        {
            get => _statisticsVisibility;
            set => SetProperty(ref _statisticsVisibility, value);
        }

        /// <summary>
        /// Gets or sets the export statistics.
        /// </summary>
        public ExportStatistics ExportStatistics
        {
            get => _exportStatistics;
            set => SetProperty(ref _exportStatistics, value);
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

        /// <summary>
        /// Gets the collection of recent paths.
        /// </summary>
        public ObservableCollection<string> RecentPaths { get; }

        /// <summary>
        /// Gets or sets the maximum file size in KB.
        /// </summary>
        public double MaxFileSizeKB
        {
            get => _settings?.MaxFileSizeKB ?? 10240;
            set
            {
                if (_settings != null)
                {
                    var clampedValue = Math.Max(0, Math.Min(10240, value));
                    if (Math.Abs(_settings.MaxFileSizeKB - clampedValue) > 0.01)
                    {
                        _settings.MaxFileSizeKB = clampedValue;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(MaxFileSizeMB));
                        _ = SaveSettingsAsync();
                        
                        // Update statistics when file size limit changes
                        _ = UpdateStatisticsAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum file size in MB for display.
        /// </summary>
        public double MaxFileSizeMB
        {
            get => MaxFileSizeKB / 1024.0;
            set => MaxFileSizeKB = value * 1024.0;
        }

        /// <summary>
        /// Gets whether file management can be performed.
        /// </summary>
        public bool CanManageFiles
        {
            get
            {
                var canManage = !string.IsNullOrWhiteSpace(RootPath) && 
                               FileExtensions.Any(x => x.IsChecked) &&
                               !IsProcessing;
                return canManage;
            }
        }

        #endregion

        #region Commands

        public ICommand BrowseRootCommand { get; private set; }
        public ICommand BrowseOutputCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }
        public ICommand ExportToClipboardCommand { get; private set; }
        public ICommand SelectRecentPathCommand { get; private set; }
        public ICommand ManageFilesCommand { get; private set; }

        public ICommand SettingsCommand { get; private set; }
        public ICommand HelpCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }

        #endregion

        #region Private Methods

        private void InitializeCommands()
        {
            BrowseRootCommand = new RelayCommand(ExecuteBrowseRoot);
            BrowseOutputCommand = new RelayCommand(ExecuteBrowseOutput);
            ExportCommand = new RelayCommand(ExecuteExport, CanExecuteExport);
            ExportToClipboardCommand = new RelayCommand(ExecuteExportToClipboard, CanExecuteExport);
            SelectRecentPathCommand = new RelayCommand<string>(ExecuteSelectRecentPath);
            ManageFilesCommand = new RelayCommand(ExecuteManageFiles, CanExecuteManageFiles);

            SettingsCommand = new RelayCommand(ExecuteSettings);
            HelpCommand = new RelayCommand(ExecuteHelp);
            AboutCommand = new RelayCommand(ExecuteAbout);
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                _settings = await _settingsService.LoadSettingsAsync();
                
                // Load recent paths
                RecentPaths.Clear();
                foreach (var path in _settings.RecentPaths.Where(Directory.Exists))
                {
                    RecentPaths.Add(path);
                }

                // Restore last paths if they exist
                if (!string.IsNullOrWhiteSpace(_settings.LastRootPath) && Directory.Exists(_settings.LastRootPath))
                {
                    RootPath = _settings.LastRootPath;
                }

                if (!string.IsNullOrWhiteSpace(_settings.LastOutputPath))
                {
                    var directory = Path.GetDirectoryName(_settings.LastOutputPath);
                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    {
                        OutputPath = _settings.LastOutputPath;
                    }
                }

                StatusMessage = "Ready. Select a root directory to begin.";
            }
            catch
            {
                _settings = new AppSettings();
                StatusMessage = "Select a root directory to begin.";
            }

            // Notify property changes for max file size
            OnPropertyChanged(nameof(MaxFileSizeKB));
            OnPropertyChanged(nameof(MaxFileSizeMB));
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
            StatisticsVisibility = Visibility.Collapsed;
            StatusMessage = "Scanning directory...";
            ProgressText = "Scanning for file types...";

            try
            {
                var extensions = await _fileService.GetFileExtensionsAsync(RootPath, _settings?.ExcludedDirectories, _settings?.AcceptedFileFormats);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var ext in extensions)
                    {
                        var item = new FileExtensionItem { Extension = ext };
                        
                        // Auto-select preferred extensions
                        if (_settings?.PreferredExtensions?.Contains(ext) == true)
                        {
                            item.IsChecked = true;
                        }
                        
                        item.PropertyChanged += OnFileExtensionItemPropertyChanged;
                        FileExtensions.Add(item);
                    }

                    if (FileExtensions.Count > 0)
                    {
                        FileTypesVisibility = Visibility.Visible;
                        StatusMessage = $"Found {FileExtensions.Count} file types. Select the ones to include.";
                        
                        // Update statistics if any extensions are selected
                        if (FileExtensions.Any(x => x.IsChecked))
                        {
                            _ = UpdateStatisticsAsync();
                        }
                    }
                    else
                    {
                        StatusMessage = "No text files found in the selected directory.";
                    }

                    OnPropertyChanged(nameof(CanExport));
                });

                // Save to recent paths
                if (_settingsService != null)
                {
                    await _settingsService.AddRecentPathAsync(RootPath);
                    
                    // Update recent paths in UI
                    if (!RecentPaths.Contains(RootPath))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            RecentPaths.Insert(0, RootPath);
                            if (RecentPaths.Count > 10)
                            {
                                RecentPaths.RemoveAt(RecentPaths.Count - 1);
                            }
                        });
                    }
                }
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
                ProgressText = string.Empty;
            }
        }

        private void OnFileExtensionItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileExtensionItem.IsChecked))
            {
                OnPropertyChanged(nameof(CanExport));
                OnPropertyChanged(nameof(CanManageFiles));
                CommandManager.InvalidateRequerySuggested();
                
                // Clear managed files when extension selection changes
                _managedFiles = null;
                
                // Update statistics when selection changes
                _ = UpdateStatisticsAsync();
                
                // Save preferred extensions
                _ = SavePreferredExtensionsAsync();
            }
        }

        private async Task SavePreferredExtensionsAsync()
        {
            if (_settings != null && _settingsService != null)
            {
                _settings.PreferredExtensions = FileExtensions
                    .Where(x => x.IsChecked)
                    .Select(x => x.Extension)
                    .ToList();
                
                await _settingsService.SaveSettingsAsync(_settings);
            }
        }

        private async Task UpdateStatisticsAsync()
        {
            if (!CanExport) 
            {
                StatisticsVisibility = Visibility.Collapsed;
                return;
            }

            try
            {
                ExportStatistics stats;
                
                if (_managedFiles != null)
                {
                    // Calculate statistics from managed files
                    var includedFiles = _managedFiles.Where(f => f.IsIncluded).ToList();
                    var totalSize = includedFiles.Sum(f => f.SizeBytes);
                    var estimatedLines = includedFiles.Sum(f => (int)(f.SizeBytes / 50)); // Rough estimate
                    
                    stats = new ExportStatistics
                    {
                        TotalFiles = includedFiles.Count,
                        TotalSizeBytes = totalSize,
                        SkippedFiles = _managedFiles.Count(f => !f.IsIncluded),
                        EstimatedLines = estimatedLines
                    };
                }
                else
                {
                    var selectedExtensions = FileExtensions
                        .Where(x => x.IsChecked)
                        .Select(x => x.Extension)
                        .ToList();

                    if (selectedExtensions.Any())
                    {
                        stats = await _fileService.GetExportStatisticsAsync(RootPath, selectedExtensions, _settings);
                    }
                    else
                    {
                        StatisticsVisibility = Visibility.Collapsed;
                        return;
                    }
                }
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ExportStatistics = stats;
                    StatisticsVisibility = Visibility.Visible;
                });
            }
            catch
            {
                // Silently fail statistics update
                StatisticsVisibility = Visibility.Collapsed;
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
            ProgressValue = 0;

            try
            {
                var selectedExtensions = FileExtensions
                    .Where(x => x.IsChecked)
                    .Select(x => x.Extension)
                    .ToList();

                var progress = new Progress<ExportProgress>(p =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressValue = p.PercentComplete;
                        ProgressText = $"Processing: {Path.GetFileName(p.CurrentFile)} ({p.FilesProcessed}/{p.TotalFiles})";
                        
                        if (!string.IsNullOrEmpty(p.ErrorMessage))
                        {
                            StatusMessage = p.ErrorMessage;
                        }
                    });
                });

                if (_managedFiles != null)
                {
                    await _fileService.ExportManagedFilesAsync(RootPath, OutputPath, _managedFiles, _settings, progress);
                }
                else
                {
                    await _fileService.ExportFilesAsync(RootPath, OutputPath, selectedExtensions, _settings, progress);
                }

                // Save settings
                if (_settings != null && _settingsService != null)
                {
                    _settings.LastRootPath = RootPath;
                    _settings.LastOutputPath = OutputPath;
                    await _settingsService.SaveSettingsAsync(_settings);
                }

                StatusMessage = "Export completed successfully!";
                ShowSuccess($"Export completed successfully!\nFile saved to: {OutputPath}");
            }
            catch (Exception ex)
            {
                ShowError($"Error during export: {ex.Message}");
                StatusMessage = "Error occurred during export.";
            }
            finally
            {
                IsProcessing = false;
                ProgressValue = 0;
                ProgressText = string.Empty;
            }
        }

        private async void ExecuteExportToClipboard()
        {
            IsProcessing = true;
            StatusMessage = "Exporting to clipboard...";
            ProgressValue = 0;

            try
            {
                var selectedExtensions = FileExtensions
                    .Where(x => x.IsChecked)
                    .Select(x => x.Extension)
                    .ToList();

                var progress = new Progress<ExportProgress>(p =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressValue = p.PercentComplete;
                        ProgressText = $"Processing: {Path.GetFileName(p.CurrentFile)} ({p.FilesProcessed}/{p.TotalFiles})";
                        
                        if (!string.IsNullOrEmpty(p.ErrorMessage))
                        {
                            StatusMessage = p.ErrorMessage;
                        }
                    });
                });

                string content;
                if (_managedFiles != null)
                {
                    content = await _fileService.ExportManagedFilesToStringAsync(RootPath, _managedFiles, _settings, progress);
                }
                else
                {
                    content = await _fileService.ExportFilesToStringAsync(RootPath, selectedExtensions, _settings, progress);
                }

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
                ProgressValue = 0;
                ProgressText = string.Empty;
            }
        }

        private void ExecuteSelectRecentPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                RootPath = path;
            }
        }

        private bool CanExecuteManageFiles()
        {
            return CanManageFiles;
        }

        private async void ExecuteManageFiles()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Loading files for management...";

                // Get all files that would be included in export
                var selectedExtensions = FileExtensions
                    .Where(x => x.IsChecked)
                    .Select(x => x.Extension)
                    .ToList();

                var files = await Task.Run(() =>
                {
                    return _fileService.GetFilteredFilesForManagement(RootPath, selectedExtensions, _settings)
                        .Select(filePath => FileItem.FromPath(filePath, RootPath))
                        .ToList();
                });

                // If we have managed files from previous session, restore their settings
                if (_managedFiles != null)
                {
                    var managedDict = _managedFiles.ToDictionary(f => f.RelativePath);
                    foreach (var file in files)
                    {
                        if (managedDict.TryGetValue(file.RelativePath, out var managedFile))
                        {
                            file.IsIncluded = managedFile.IsIncluded;
                            file.IncludeContent = managedFile.IncludeContent;
                        }
                    }
                }

                var viewModel = new FileManagementViewModel(files);
                var window = new Views.FileManagementWindow(viewModel)
                {
                    Owner = Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    // Save the managed files settings
                    _managedFiles = viewModel.AllFiles.ToList();
                    StatusMessage = "File management settings saved.";
                    
                    // Update statistics to reflect changes
                    _ = UpdateStatisticsAsync();
                }
                else
                {
                    StatusMessage = "File management cancelled.";
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error loading files for management: {ex.Message}");
                StatusMessage = "Error occurred during file management.";
            }
            finally
            {
                IsProcessing = false;
            }
        }



        private void ExecuteSettings()
        {
            var settingsViewModel = new SettingsViewModel(_settingsService, _settings);
            var settingsWindow = new Views.SettingsWindow(settingsViewModel)
            {
                Owner = Application.Current.MainWindow
            };

            if (settingsWindow.ShowDialog() == true)
            {
                // Settings were saved, refresh statistics if applicable
                if (CanExport)
                {
                    _ = UpdateStatisticsAsync();
                }
            }
        }

        private void ExecuteHelp()
        {
            var helpWindow = new Views.HelpWindow()
            {
                Owner = Application.Current.MainWindow
            };
            helpWindow.ShowDialog();
        }

        private void ExecuteAbout()
        {
            var aboutMessage = "Source Code Gatherer\n\n" +
                              "A tool for collecting and exporting source code files from a directory structure.\n\n" +
                              "Features:\n" +
                              "• Customizable file filtering\n" +
                              "• Multiple export formats\n" +
                              "• File size limits\n" +
                              "• Streaming support for large exports\n\n" +
                              "Version 1.0";

            MessageBox.Show(aboutMessage, "About Source Code Gatherer", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateDefaultOutputPath()
        {
            if (!string.IsNullOrWhiteSpace(RootPath) && Directory.Exists(RootPath))
            {
                var projectName = GetProjectName(RootPath);
                var downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
                OutputPath = Path.Combine(downloadsPath, $"{projectName}.txt");
                OnPropertyChanged(nameof(CanExport));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// Gets the project name by detecting common project files, falling back to folder name.
        /// </summary>
        /// <param name="rootPath">The root directory path.</param>
        /// <returns>The project name or folder name as fallback.</returns>
        private string GetProjectName(string rootPath)
        {
            try
            {
                // Define project file patterns in order of preference
                var projectFilePatterns = new[]
                {
                    "*.sln",        // Visual Studio Solution
                    "*.csproj",     // C# Project
                    "*.vbproj",     // VB.NET Project
                    "*.fsproj",     // F# Project
                    "package.json", // Node.js/JavaScript
                    "pom.xml",      // Java Maven
                    "build.gradle", // Java Gradle
                    "Cargo.toml",   // Rust
                    "go.mod",       // Go
                    "pyproject.toml", // Python
                    "setup.py",     // Python
                    "composer.json", // PHP
                    "Gemfile",      // Ruby
                    "mix.exs"       // Elixir
                };

                foreach (var pattern in projectFilePatterns)
                {
                    var files = Directory.GetFiles(rootPath, pattern, SearchOption.TopDirectoryOnly);
                    if (files.Length > 0)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(files[0]);
                        
                        // For package.json, try to get the name from the JSON content
                        if (pattern == "package.json")
                        {
                            var packageName = GetPackageJsonName(files[0]);
                            if (!string.IsNullOrWhiteSpace(packageName))
                                return SanitizeFileName(packageName);
                        }
                        
                        return SanitizeFileName(fileName);
                    }
                }
            }
            catch
            {
                // If any error occurs during project detection, fall back to folder name
            }

            // Fallback to folder name
            return SanitizeFileName(new DirectoryInfo(rootPath).Name);
        }

        /// <summary>
        /// Extracts the project name from package.json file.
        /// </summary>
        /// <param name="packageJsonPath">Path to package.json file.</param>
        /// <returns>The project name from package.json or null if not found.</returns>
        private string GetPackageJsonName(string packageJsonPath)
        {
            try
            {
                var content = File.ReadAllText(packageJsonPath);
                // Simple JSON parsing for the name field
                var nameMatch = System.Text.RegularExpressions.Regex.Match(
                    content, @"""name""\s*:\s*""([^""]+)""", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (nameMatch.Success)
                {
                    return nameMatch.Groups[1].Value;
                }
            }
            catch
            {
                // Ignore errors and return null
            }
            
            return null;
        }

        /// <summary>
        /// Sanitizes a filename by removing invalid characters.
        /// </summary>
        /// <param name="fileName">The filename to sanitize.</param>
        /// <returns>A sanitized filename safe for use in file paths.</returns>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "project";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            
            // Replace common problematic characters with underscores
            sanitized = sanitized.Replace(' ', '_').Replace('-', '_');
            
            return string.IsNullOrWhiteSpace(sanitized) ? "project" : sanitized;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowSuccess(string message)
        {
            MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task SaveSettingsAsync()
        {
            if (_settings != null && _settingsService != null)
            {
                try
                {
                    await _settingsService.SaveSettingsAsync(_settings);
                }
                catch
                {
                    // Silently fail - settings are not critical
                }
            }
        }

        #endregion
    }
}
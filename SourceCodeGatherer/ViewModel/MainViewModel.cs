using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SourceCodeGatherer
{
    /// <summary>
    /// ViewModel for the main window, implementing MVVM pattern for source code gathering.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _rootPath;
        private string _outputPath;
        private bool _isProcessing;
        private string _statusMessage;
        private Visibility _progressVisibility = Visibility.Collapsed;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            FileExtensions = new ObservableCollection<FileExtensionItem>();
            BrowseRootCommand = new RelayCommand(ExecuteBrowseRoot);
            BrowseOutputCommand = new RelayCommand(ExecuteBrowseOutput);
            ScanDirectoryCommand = new RelayCommand(ExecuteScanDirectory, CanExecuteScanDirectory);
            ExportCommand = new RelayCommand(ExecuteExport, CanExecuteExport);
            StatusMessage = "Select a root directory to begin.";
        }

        /// <summary>
        /// Gets or sets the root path for scanning source files.
        /// </summary>
        public string RootPath
        {
            get => _rootPath;
            set
            {
                _rootPath = value;
                OnPropertyChanged();
                UpdateDefaultOutputPath();
                OnPropertyChanged(nameof(CanExport));
                CommandManager.InvalidateRequerySuggested();
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
                _outputPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanExport));
                CommandManager.InvalidateRequerySuggested();
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
                _isProcessing = value;
                OnPropertyChanged();
                ProgressVisibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets or sets the status message displayed to the user.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the visibility of the progress bar.
        /// </summary>
        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set
            {
                _progressVisibility = value;
                OnPropertyChanged();
            }
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

                // Update status message to guide user
                if (!string.IsNullOrWhiteSpace(RootPath) && !string.IsNullOrWhiteSpace(OutputPath))
                {
                    if (FileExtensions.Count == 0)
                    {
                        StatusMessage = "Click 'Scan Directory' to find file types.";
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

                return canExport;
            }
        }

        /// <summary>
        /// Gets the collection of file extensions found in the directory.
        /// </summary>
        public ObservableCollection<FileExtensionItem> FileExtensions { get; }

        public ICommand BrowseRootCommand { get; }
        public ICommand BrowseOutputCommand { get; }
        public ICommand ScanDirectoryCommand { get; }
        public ICommand ExportCommand { get; }

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

        private bool CanExecuteScanDirectory()
        {
            return !string.IsNullOrWhiteSpace(RootPath) && Directory.Exists(RootPath);
        }

        private async void ExecuteScanDirectory()
        {
            IsProcessing = true;
            FileExtensions.Clear();
            StatusMessage = "Scanning directory...";

            try
            {
                await Task.Run(() =>
                {
                    var extensions = Directory.GetFiles(RootPath, "*.*", SearchOption.AllDirectories)
                        .Select(f => Path.GetExtension(f).ToLower())
                        .Where(ext => !string.IsNullOrWhiteSpace(ext) && IsTextFile(ext))
                        .Distinct()
                        .OrderBy(ext => ext);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var ext in extensions)
                        {
                            var item = new FileExtensionItem { Extension = ext };
                            item.PropertyChanged += (s, e) =>
                            {
                                if (e.PropertyName == nameof(FileExtensionItem.IsChecked))
                                {
                                    OnPropertyChanged(nameof(CanExport));
                                    CommandManager.InvalidateRequerySuggested();
                                }
                            };
                            FileExtensions.Add(item);
                        }
                        OnPropertyChanged(nameof(CanExport));
                    });
                });

                StatusMessage = $"Found {FileExtensions.Count} file types.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning directory: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error occurred during scanning.";
            }
            finally
            {
                IsProcessing = false;
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

                await Task.Run(() =>
                {
                    using (var writer = new StreamWriter(OutputPath, false, Encoding.UTF8))
                    {
                        var files = Directory.GetFiles(RootPath, "*.*", SearchOption.AllDirectories)
                            .Where(f => selectedExtensions.Contains(Path.GetExtension(f).ToLower()))
                            .OrderBy(f => f);

                        foreach (var file in files)
                        {
                            var relativePath = Path.GetRelativePath(RootPath, file);
                            writer.WriteLine($"=== FILE: {relativePath} ===");
                            writer.WriteLine();

                            try
                            {
                                var content = File.ReadAllText(file);
                                writer.WriteLine(content);
                            }
                            catch (Exception ex)
                            {
                                writer.WriteLine($"[ERROR READING FILE: {ex.Message}]");
                            }

                            writer.WriteLine();
                            writer.WriteLine("=== END OF FILE ===");
                            writer.WriteLine();
                        }
                    }
                });

                StatusMessage = "Export completed successfully!";
                MessageBox.Show("Export completed successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during export: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error occurred during export.";
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
                var time = DateTime.Now.ToString("HH:mm:ss");
                OutputPath = Path.Combine(downloadsPath, $"{rootDirName}_{date}_{time}.txt");
                OnPropertyChanged(nameof(CanExport));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool IsTextFile(string extension)
        {
            var textExtensions = new[]
            {
                ".cs", ".py", ".js", ".ts", ".jsx", ".tsx", ".java", ".cpp", ".c", ".h",
                ".hpp", ".xml", ".json", ".yaml", ".yml", ".md", ".txt", ".html", ".css",
                ".scss", ".sass", ".less", ".sql", ".sh", ".bat", ".ps1", ".rb", ".go",
                ".rs", ".swift", ".kt", ".php", ".r", ".m", ".mm", ".scala", ".groovy",
                ".lua", ".dart", ".vue", ".svelte", ".astro", ".ini", ".config", ".conf",
                ".toml", ".properties", ".env", ".gitignore", ".dockerignore", ".editorconfig"
            };

            return textExtensions.Contains(extension.ToLower());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a file extension item with checkbox state.
    /// </summary>
    public class FileExtensionItem : INotifyPropertyChanged
    {
        private string _extension;
        private bool _isChecked;

        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        public string Extension
        {
            get => _extension;
            set
            {
                _extension = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether this extension is selected.
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Basic relay command implementation for MVVM pattern.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of the RelayCommand class.
        /// </summary>
        /// <param name="execute">The action to execute.</param>
        /// <param name="canExecute">The function to determine if command can execute.</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using SourceCodeGatherer.Commands;
using SourceCodeGatherer.Models;

namespace SourceCodeGatherer.ViewModels
{
    /// <summary>
    /// ViewModel for the file management window.
    /// </summary>
    public class FileManagementViewModel : BaseViewModel
    {
        private string _searchText = string.Empty;

        private readonly ObservableCollection<FileItem> _allFiles;
        private ICollectionView _filteredFilesView;

        public FileManagementViewModel(IEnumerable<FileItem> files)
        {
            _allFiles = new ObservableCollection<FileItem>(files);
            
            // Subscribe to property changes on file items
            foreach (var file in _allFiles)
            {
                file.PropertyChanged += OnFileItemPropertyChanged;
            }
            
            InitializeCommands();
            InitializeCollectionView();
            UpdateCounts();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the search text for filtering files.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _filteredFilesView?.Refresh();
                }
            }
        }



        /// <summary>
        /// Gets the filtered files collection view.
        /// </summary>
        public ICollectionView FilteredFiles => _filteredFilesView;

        /// <summary>
        /// Gets the status text showing file counts.
        /// </summary>
        public string StatusText => $"Total files: {_allFiles.Count}";

        /// <summary>
        /// Gets the count of included files.
        /// </summary>
        public int IncludedFilesCount => _allFiles.Count(f => f.IsIncluded);

        /// <summary>
        /// Gets the count of files with content included.
        /// </summary>
        public int ContentIncludedCount => _allFiles.Count(f => f.IncludeContent);

        /// <summary>
        /// Gets all file items (for external access).
        /// </summary>
        public IEnumerable<FileItem> AllFiles => _allFiles;

        #endregion

        #region Commands

        public ICommand ClearSearchCommand { get; private set; }
        public ICommand SelectAllCommand { get; private set; }
        public ICommand SelectNoneCommand { get; private set; }
        public ICommand IncludeAllContentCommand { get; private set; }
        public ICommand ExcludeAllContentCommand { get; private set; }
        public ICommand OkCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when the window should be closed.
        /// </summary>
        public event EventHandler<bool> CloseRequested;

        #endregion

        #region Private Methods

        private void InitializeCommands()
        {
            ClearSearchCommand = new RelayCommand(ExecuteClearSearch);
            SelectAllCommand = new RelayCommand(ExecuteSelectAll);
            SelectNoneCommand = new RelayCommand(ExecuteSelectNone);
            IncludeAllContentCommand = new RelayCommand(ExecuteIncludeAllContent);
            ExcludeAllContentCommand = new RelayCommand(ExecuteExcludeAllContent);
            OkCommand = new RelayCommand(ExecuteOk);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private void InitializeCollectionView()
        {
            _filteredFilesView = CollectionViewSource.GetDefaultView(_allFiles);
            _filteredFilesView.Filter = FilterFiles;
        }

        private bool FilterFiles(object item)
        {
            if (item is not FileItem file)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var searchLower = SearchText.ToLower();
            return file.FileName.ToLower().Contains(searchLower) ||
                   file.RelativeDirectory.ToLower().Contains(searchLower) ||
                   file.FileType.ToLower().Contains(searchLower);
        }



        private void OnFileItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileItem.IsIncluded) || 
                e.PropertyName == nameof(FileItem.IncludeContent))
            {
                UpdateCounts();
            }
        }

        private void UpdateCounts()
        {
            OnPropertyChanged(nameof(IncludedFilesCount));
            OnPropertyChanged(nameof(ContentIncludedCount));
        }

        private void ExecuteClearSearch()
        {
            SearchText = string.Empty;
        }

        private void ExecuteSelectAll()
        {
            foreach (var file in _allFiles)
            {
                file.IsIncluded = true;
            }
        }

        private void ExecuteSelectNone()
        {
            foreach (var file in _allFiles)
            {
                file.IsIncluded = false;
            }
        }

        private void ExecuteIncludeAllContent()
        {
            foreach (var file in _allFiles.Where(f => f.IsIncluded))
            {
                file.IncludeContent = true;
            }
        }

        private void ExecuteExcludeAllContent()
        {
            foreach (var file in _allFiles)
            {
                file.IncludeContent = false;
            }
        }

        private void ExecuteOk()
        {
            CloseRequested?.Invoke(this, true);
        }

        private void ExecuteCancel()
        {
            CloseRequested?.Invoke(this, false);
        }

        #endregion
    }
}
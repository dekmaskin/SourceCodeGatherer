using System;
using System.ComponentModel;
using System.IO;

namespace SourceCodeGatherer.Models
{
    /// <summary>
    /// Represents a file item with exclusion options for export management.
    /// </summary>
    public class FileItem : INotifyPropertyChanged
    {
        private bool _isIncluded = true;
        private bool _includeContent = true;

        /// <summary>
        /// Gets or sets the full file path.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Gets or sets the relative file path from the root directory.
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets the relative directory path without the file name.
        /// </summary>
        public string RelativeDirectory
        {
            get
            {
                var directory = Path.GetDirectoryName(RelativePath);
                return string.IsNullOrEmpty(directory) ? "." : directory;
            }
        }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long SizeBytes { get; set; }

        /// <summary>
        /// Gets the formatted file size.
        /// </summary>
        public string FormattedSize
        {
            get
            {
                if (SizeBytes < 1024)
                    return $"{SizeBytes} B";
                if (SizeBytes < 1024 * 1024)
                    return $"{SizeBytes / 1024.0:F1} KB";
                return $"{SizeBytes / (1024.0 * 1024.0):F1} MB";
            }
        }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets or sets whether this file should be included in the export.
        /// </summary>
        public bool IsIncluded
        {
            get => _isIncluded;
            set
            {
                if (_isIncluded != value)
                {
                    _isIncluded = value;
                    OnPropertyChanged(nameof(IsIncluded));
                    
                    // If file is excluded, also exclude content
                    if (!value && _includeContent)
                    {
                        _includeContent = false;
                        OnPropertyChanged(nameof(IncludeContent));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the file content should be included (file name will still appear).
        /// </summary>
        public bool IncludeContent
        {
            get => _includeContent;
            set
            {
                if (_includeContent != value)
                {
                    _includeContent = value;
                    OnPropertyChanged(nameof(IncludeContent));
                    
                    // If content is included, file must also be included
                    if (value && !_isIncluded)
                    {
                        _isIncluded = true;
                        OnPropertyChanged(nameof(IsIncluded));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the file type category for grouping.
        /// </summary>
        public string FileType
        {
            get
            {
                return Extension.ToLower() switch
                {
                    ".cs" or ".vb" or ".fs" => "C#/VB.NET",
                    ".js" or ".ts" or ".jsx" or ".tsx" => "JavaScript/TypeScript",
                    ".py" => "Python",
                    ".java" => "Java",
                    ".cpp" or ".c" or ".h" or ".hpp" => "C/C++",
                    ".html" or ".htm" => "HTML",
                    ".css" or ".scss" or ".sass" or ".less" => "CSS",
                    ".xml" or ".xaml" => "XML",
                    ".json" => "JSON",
                    ".yaml" or ".yml" => "YAML",
                    ".md" => "Markdown",
                    ".txt" => "Text",
                    ".sql" => "SQL",
                    ".sh" or ".bat" or ".ps1" => "Scripts",
                    _ => "Other"
                };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Creates a FileItem from a file path and root directory.
        /// </summary>
        public static FileItem FromPath(string filePath, string rootPath)
        {
            var fileInfo = new FileInfo(filePath);
            return new FileItem
            {
                FullPath = filePath,
                RelativePath = Path.GetRelativePath(rootPath, filePath),
                FileName = fileInfo.Name,
                Extension = fileInfo.Extension,
                SizeBytes = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime
            };
        }
    }
}
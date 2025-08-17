using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SourceCodeGatherer.Models;

namespace SourceCodeGatherer.Services
{
    /// <summary>
    /// Interface for file operations service.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Gets all unique file extensions in a directory.
        /// </summary>
        /// <param name="rootPath">The root directory path.</param>
        /// <param name="excludedDirectories">Directory patterns to exclude.</param>
        /// <returns>Collection of unique file extensions.</returns>
        Task<IEnumerable<string>> GetFileExtensionsAsync(string rootPath, IEnumerable<string> excludedDirectories = null);

        /// <summary>
        /// Exports files with specified extensions to a text file.
        /// </summary>
        /// <param name="rootPath">The root directory path.</param>
        /// <param name="outputPath">The output file path.</param>
        /// <param name="selectedExtensions">Extensions to include.</param>
        /// <param name="settings">Application settings for filtering and limits.</param>
        /// <param name="progress">Progress reporter.</param>
        Task ExportFilesAsync(string rootPath, string outputPath, IEnumerable<string> selectedExtensions, 
            AppSettings settings = null, IProgress<ExportProgress> progress = null);

        /// <summary>
        /// Exports files with specified extensions to a string.
        /// </summary>
        /// <param name="rootPath">The root directory path.</param>
        /// <param name="selectedExtensions">Extensions to include.</param>
        /// <param name="settings">Application settings for filtering and limits.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <returns>The exported content as a string.</returns>
        Task<string> ExportFilesToStringAsync(string rootPath, IEnumerable<string> selectedExtensions,
            AppSettings settings = null, IProgress<ExportProgress> progress = null);

        /// <summary>
        /// Gets export statistics for the specified parameters.
        /// </summary>
        /// <param name="rootPath">The root directory path.</param>
        /// <param name="selectedExtensions">Extensions to include.</param>
        /// <param name="settings">Application settings for filtering and limits.</param>
        /// <returns>Export statistics.</returns>
        Task<ExportStatistics> GetExportStatisticsAsync(string rootPath, IEnumerable<string> selectedExtensions,
            AppSettings settings = null);

        /// <summary>
        /// Determines if a file extension represents a text file.
        /// </summary>
        /// <param name="extension">The file extension.</param>
        /// <returns>True if text file, false otherwise.</returns>
        bool IsTextFile(string extension);

        /// <summary>
        /// Determines if a file appears to be binary by checking its content.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>True if binary, false if text.</returns>
        Task<bool> IsBinaryFileAsync(string filePath);
    }
}
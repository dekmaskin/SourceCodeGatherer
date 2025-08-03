using System.Collections.Generic;
using System.Threading.Tasks;

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
        /// <returns>Collection of unique file extensions.</returns>
        Task<IEnumerable<string>> GetFileExtensionsAsync(string rootPath);

        /// <summary>
        /// Exports files with specified extensions to a text file.
        /// </summary>
        /// <param name="rootPath">The root directory path.</param>
        /// <param name="outputPath">The output file path.</param>
        /// <param name="selectedExtensions">Extensions to include.</param>
        Task ExportFilesAsync(string rootPath, string outputPath, IEnumerable<string> selectedExtensions);

        /// <summary>
        /// Exports files with specified extensions to a string.
        /// </summary>
        /// <param name="rootPath">The root directory path.</param>
        /// <param name="selectedExtensions">Extensions to include.</param>
        /// <returns>The exported content as a string.</returns>
        Task<string> ExportFilesToStringAsync(string rootPath, IEnumerable<string> selectedExtensions);

        /// <summary>
        /// Determines if a file extension represents a text file.
        /// </summary>
        /// <param name="extension">The file extension.</param>
        /// <returns>True if text file, false otherwise.</returns>
        bool IsTextFile(string extension);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SourceCodeGatherer.Models;

namespace SourceCodeGatherer.Services
{
    /// <summary>
    /// Service for handling file operations with enhanced filtering and performance.
    /// </summary>
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly HashSet<string> _defaultTextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".py", ".js", ".ts", ".jsx", ".tsx", ".java", ".cpp", ".c", ".h",
            ".hpp", ".xml", ".json", ".yaml", ".yml", ".md", ".txt", ".html", ".css",
            ".scss", ".sass", ".less", ".sql", ".sh", ".bat", ".ps1", ".rb", ".go",
            ".rs", ".swift", ".kt", ".php", ".r", ".m", ".mm", ".scala", ".groovy",
            ".lua", ".dart", ".vue", ".svelte", ".astro", ".ini", ".config", ".conf",
            ".toml", ".properties", ".env", ".gitignore", ".dockerignore", ".editorconfig",
            ".csv", ".log", ".diff", ".patch", ".asm", ".pl", ".pm", ".hs", ".clj",
            ".razor", ".fs", ".vb", ".vbs", ".asmx", ".aspx", ".jsp", ".jspx", ".makefile"
        };

        /// <summary>
        /// Initializes a new instance of the FileService class.
        /// </summary>
        public FileService()
        {
            _logger = App.LoggingService?.GetLogger<FileService>() ?? 
                     Microsoft.Extensions.Logging.Abstractions.NullLogger<FileService>.Instance;
            _logger.LogDebug("FileService initialized");
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetFileExtensionsAsync(string rootPath, IEnumerable<string> excludedDirectories = null, IEnumerable<string> acceptedFormats = null)
        {
            _logger.LogInformation("Starting file extension scan for path: {RootPath}", rootPath);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                return await Task.Run(() =>
                {
                    var excludedDirs = excludedDirectories?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
                    _logger.LogDebug("Excluded directories: {ExcludedDirs}", string.Join(", ", excludedDirs));

                    var extensions = GetFilteredFiles(rootPath, excludedDirs, null, long.MaxValue, acceptedFormats)
                        .Select(f => Path.GetExtension(f).ToLower())
                        .Where(ext => !string.IsNullOrWhiteSpace(ext) && IsAcceptedFile(ext, acceptedFormats))
                        .Distinct()
                        .OrderBy(ext => ext)
                        .ToList();

                    _logger.LogInformation("Found {ExtensionCount} unique file extensions in {ElapsedMs}ms", 
                                         extensions.Count, stopwatch.ElapsedMilliseconds);
                    _logger.LogDebug("Extensions found: {Extensions}", string.Join(", ", extensions));

                    return extensions;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file extensions for path: {RootPath}", rootPath);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task ExportFilesAsync(string rootPath, string outputPath, IEnumerable<string> selectedExtensions, 
            AppSettings settings = null, IProgress<ExportProgress> progress = null)
        {
            settings ??= new AppSettings();
            var extensionList = selectedExtensions.ToList();
            
            _logger.LogInformation("Starting file export from {RootPath} to {OutputPath}", rootPath, outputPath);
            _logger.LogInformation("Selected extensions: {Extensions}", string.Join(", ", extensionList));
            _logger.LogInformation("Using streaming: {UseStreaming}, Max file size: {MaxFileSize} bytes", 
                                 settings.UseStreaming, settings.MaxFileSizeBytes);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (settings.UseStreaming)
                {
                    await ExportFilesStreamAsync(rootPath, outputPath, extensionList, settings, progress);
                }
                else
                {
                    var content = await ExportFilesToStringAsync(rootPath, extensionList, settings, progress);
                    await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8);
                }

                _logger.LogInformation("File export completed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file export from {RootPath} to {OutputPath}", rootPath, outputPath);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task<string> ExportFilesToStringAsync(string rootPath, IEnumerable<string> selectedExtensions,
            AppSettings settings = null, IProgress<ExportProgress> progress = null)
        {
            settings ??= new AppSettings();
            var extensionSet = new HashSet<string>(selectedExtensions, StringComparer.OrdinalIgnoreCase);

            _logger.LogDebug("Starting export to string for {RootPath}", rootPath);

            return await Task.Run(async () =>
            {
                using var writer = new StringWriter();
                var files = GetFilteredFiles(rootPath, settings.ExcludedDirectories, extensionSet, settings.MaxFileSizeBytes, settings.AcceptedFileFormats).ToList();
                
                _logger.LogInformation("Processing {FileCount} files for string export", files.Count);
                
                var progressInfo = new ExportProgress { TotalFiles = files.Count };
                var errorCount = 0;
                
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    progressInfo.CurrentFile = Path.GetRelativePath(rootPath, file);
                    progressInfo.FilesProcessed = i;
                    progress?.Report(progressInfo);

                    try
                    {
                        await WriteFileContentAsync(writer, rootPath, file, settings.MaxFileSizeBytes);
                        var fileInfo = new FileInfo(file);
                        progressInfo.BytesProcessed += fileInfo.Length;
                        
                        if (i % 50 == 0) // Log progress every 50 files
                        {
                            _logger.LogDebug("Processed {ProcessedFiles}/{TotalFiles} files", i + 1, files.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogWarning(ex, "Error processing file {FilePath}", file);
                        progressInfo.ErrorMessage = $"Error processing {file}: {ex.Message}";
                        progress?.Report(progressInfo);
                    }
                }

                progressInfo.FilesProcessed = files.Count;
                progress?.Report(progressInfo);
                
                _logger.LogInformation("String export completed. Processed: {ProcessedFiles}, Errors: {ErrorCount}", 
                                     files.Count, errorCount);
                
                return writer.ToString();
            });
        }

        /// <inheritdoc/>
        public async Task<ExportStatistics> GetExportStatisticsAsync(string rootPath, IEnumerable<string> selectedExtensions,
            AppSettings settings = null)
        {
            settings ??= new AppSettings();
            var extensionSet = new HashSet<string>(selectedExtensions, StringComparer.OrdinalIgnoreCase);

            _logger.LogDebug("Calculating export statistics for {RootPath}", rootPath);

            return await Task.Run(() =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var allFiles = GetFilteredFiles(rootPath, settings.ExcludedDirectories, extensionSet, long.MaxValue, settings.AcceptedFileFormats).ToList();
                var validFiles = new List<string>();
                var skippedFiles = 0;
                long totalSize = 0;
                int estimatedLines = 0;
                var errorCount = 0;

                foreach (var file in allFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Length <= settings.MaxFileSizeBytes)
                        {
                            validFiles.Add(file);
                            totalSize += fileInfo.Length;
                            // Rough estimate: 50 characters per line on average
                            estimatedLines += (int)(fileInfo.Length / 50);
                        }
                        else
                        {
                            skippedFiles++;
                            _logger.LogDebug("File {FilePath} skipped due to size: {FileSize} bytes", file, fileInfo.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        skippedFiles++;
                        errorCount++;
                        _logger.LogWarning(ex, "Error accessing file {FilePath} for statistics", file);
                    }
                }

                var statistics = new ExportStatistics
                {
                    TotalFiles = validFiles.Count,
                    TotalSizeBytes = totalSize,
                    SkippedFiles = skippedFiles,
                    EstimatedLines = estimatedLines
                };

                _logger.LogInformation("Export statistics calculated in {ElapsedMs}ms: {ValidFiles} valid files, {SkippedFiles} skipped, {TotalSizeMB:F2} MB total, {Errors} errors",
                                     stopwatch.ElapsedMilliseconds, validFiles.Count, skippedFiles, totalSize / 1024.0 / 1024.0, errorCount);

                return statistics;
            });
        }

        /// <inheritdoc/>
        public bool IsTextFile(string extension)
        {
            return _defaultTextExtensions.Contains(extension);
        }

        /// <summary>
        /// Gets filtered files for management interface.
        /// </summary>
        public IEnumerable<string> GetFilteredFilesForManagement(string rootPath, IEnumerable<string> selectedExtensions, AppSettings settings = null)
        {
            settings ??= new AppSettings();
            var extensionSet = new HashSet<string>(selectedExtensions, StringComparer.OrdinalIgnoreCase);
            
            return GetFilteredFiles(rootPath, settings.ExcludedDirectories, extensionSet, long.MaxValue, settings.AcceptedFileFormats);
        }

        /// <summary>
        /// Exports managed files to a file with exclusion settings.
        /// </summary>
        public async Task ExportManagedFilesAsync(string rootPath, string outputPath, IEnumerable<FileItem> managedFiles, 
            AppSettings settings = null, IProgress<ExportProgress> progress = null)
        {
            settings ??= new AppSettings();
            
            if (settings.UseStreaming)
            {
                await ExportManagedFilesStreamAsync(rootPath, outputPath, managedFiles, settings, progress);
            }
            else
            {
                var content = await ExportManagedFilesToStringAsync(rootPath, managedFiles, settings, progress);
                await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Exports managed files to a string with exclusion settings.
        /// </summary>
        public async Task<string> ExportManagedFilesToStringAsync(string rootPath, IEnumerable<FileItem> managedFiles,
            AppSettings settings = null, IProgress<ExportProgress> progress = null)
        {
            settings ??= new AppSettings();
            var includedFiles = managedFiles.Where(f => f.IsIncluded).ToList();

            return await Task.Run(async () =>
            {
                using var writer = new StringWriter();
                var progressInfo = new ExportProgress { TotalFiles = includedFiles.Count };
                
                for (int i = 0; i < includedFiles.Count; i++)
                {
                    var fileItem = includedFiles[i];
                    progressInfo.CurrentFile = fileItem.RelativePath;
                    progressInfo.FilesProcessed = i;
                    progress?.Report(progressInfo);

                    try
                    {
                        await WriteManagedFileContentAsync(writer, rootPath, fileItem, settings.MaxFileSizeBytes);
                        progressInfo.BytesProcessed += fileItem.SizeBytes;
                    }
                    catch (Exception ex)
                    {
                        progressInfo.ErrorMessage = $"Error processing {fileItem.RelativePath}: {ex.Message}";
                        progress?.Report(progressInfo);
                    }
                }

                progressInfo.FilesProcessed = includedFiles.Count;
                progress?.Report(progressInfo);
                
                return writer.ToString();
            });
        }

        /// <summary>
        /// Exports managed files using streaming for better memory efficiency.
        /// </summary>
        private async Task ExportManagedFilesStreamAsync(string rootPath, string outputPath, IEnumerable<FileItem> managedFiles,
            AppSettings settings, IProgress<ExportProgress> progress)
        {
            var includedFiles = managedFiles.Where(f => f.IsIncluded).ToList();
            var progressInfo = new ExportProgress { TotalFiles = includedFiles.Count };

            using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(output, Encoding.UTF8);

            for (int i = 0; i < includedFiles.Count; i++)
            {
                var fileItem = includedFiles[i];
                progressInfo.CurrentFile = fileItem.RelativePath;
                progressInfo.FilesProcessed = i;
                progress?.Report(progressInfo);

                try
                {
                    await WriteManagedFileContentAsync(writer, rootPath, fileItem, settings.MaxFileSizeBytes);
                    progressInfo.BytesProcessed += fileItem.SizeBytes;
                }
                catch (Exception ex)
                {
                    progressInfo.ErrorMessage = $"Error processing {fileItem.RelativePath}: {ex.Message}";
                    progress?.Report(progressInfo);
                }
            }

            progressInfo.FilesProcessed = includedFiles.Count;
            progress?.Report(progressInfo);
        }

        /// <summary>
        /// Writes managed file content to the output stream with exclusion settings.
        /// </summary>
        private static async Task WriteManagedFileContentAsync(TextWriter writer, string rootPath, FileItem fileItem, long maxFileSize)
        {
            await writer.WriteLineAsync($"=== FILE: {fileItem.RelativePath} ===");
            await writer.WriteLineAsync();

            try
            {
                if (!fileItem.IncludeContent)
                {
                    await writer.WriteLineAsync("[CONTENT EXCLUDED BY USER]");
                }
                else if (fileItem.SizeBytes > maxFileSize)
                {
                    await writer.WriteLineAsync($"[FILE TOO LARGE: {fileItem.SizeBytes:N0} bytes, limit is {maxFileSize:N0} bytes]");
                }
                else
                {
                    var content = await File.ReadAllTextAsync(fileItem.FullPath);
                    await writer.WriteLineAsync(content);
                }
            }
            catch (UnauthorizedAccessException)
            {
                await writer.WriteLineAsync("[ERROR: Access denied]");
            }
            catch (IOException ex)
            {
                await writer.WriteLineAsync($"[ERROR: File in use or locked - {ex.Message}]");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"[ERROR READING FILE: {ex.Message}]");
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync("=== END OF FILE ===");
            await writer.WriteLineAsync();
        }

        /// <summary>
        /// Checks if a file extension is accepted based on settings.
        /// </summary>
        /// <param name="extension">The file extension to check.</param>
        /// <param name="acceptedFormats">List of accepted file formats from settings.</param>
        /// <returns>True if the extension is accepted.</returns>
        public bool IsAcceptedFile(string extension, IEnumerable<string> acceptedFormats)
        {
            if (acceptedFormats == null || !acceptedFormats.Any())
                return IsTextFile(extension);
            
            return acceptedFormats.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public async Task<bool> IsBinaryFileAsync(string filePath)
        {
            try
            {
                const int bytesToCheck = 8000;
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[Math.Min(bytesToCheck, stream.Length)];
                await stream.ReadAsync(buffer, 0, buffer.Length);

                // Check for null bytes which typically indicate binary files
                return buffer.Any(b => b == 0);
            }
            catch
            {
                return true; // Assume binary if we can't read it
            }
        }

        /// <summary>
        /// Exports files using streaming for better memory efficiency.
        /// </summary>
        private async Task ExportFilesStreamAsync(string rootPath, string outputPath, IEnumerable<string> selectedExtensions,
            AppSettings settings, IProgress<ExportProgress> progress)
        {
            var extensionSet = new HashSet<string>(selectedExtensions, StringComparer.OrdinalIgnoreCase);
            var files = GetFilteredFiles(rootPath, settings.ExcludedDirectories, extensionSet, settings.MaxFileSizeBytes, settings.AcceptedFileFormats).ToList();
            
            var progressInfo = new ExportProgress { TotalFiles = files.Count };

            using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(output, Encoding.UTF8);

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                progressInfo.CurrentFile = Path.GetRelativePath(rootPath, file);
                progressInfo.FilesProcessed = i;
                progress?.Report(progressInfo);

                try
                {
                    await WriteFileContentAsync(writer, rootPath, file, settings.MaxFileSizeBytes);
                    var fileInfo = new FileInfo(file);
                    progressInfo.BytesProcessed += fileInfo.Length;
                }
                catch (Exception ex)
                {
                    progressInfo.ErrorMessage = $"Error processing {file}: {ex.Message}";
                    progress?.Report(progressInfo);
                }
            }

            progressInfo.FilesProcessed = files.Count;
            progress?.Report(progressInfo);
        }

        /// <summary>
        /// Gets filtered files based on extensions, excluded directories, and size limits.
        /// </summary>
        private IEnumerable<string> GetFilteredFiles(string rootPath, IEnumerable<string> excludedDirectories, 
            HashSet<string> extensionSet, long maxFileSize, IEnumerable<string> acceptedFormats)
        {
            var excludedDirs = excludedDirectories?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

            return Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    // Check if file is in excluded directory
                    var relativePath = Path.GetRelativePath(rootPath, file);
                    var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    
                    if (pathParts.Any(part => excludedDirs.Contains(part)))
                        return false;

                    var extension = Path.GetExtension(file);

                    // Check if file format is accepted
                    if (!IsAcceptedFile(extension, acceptedFormats))
                        return false;

                    // Check extension if specified
                    if (extensionSet != null && !extensionSet.Contains(extension))
                        return false;

                    // Check file size
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        return fileInfo.Length <= maxFileSize;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .OrderBy(f => f);
        }

        /// <summary>
        /// Writes file content to the output stream with enhanced error handling.
        /// </summary>
        private async Task WriteFileContentAsync(TextWriter writer, string rootPath, string filePath, long maxFileSize)
        {
            var relativePath = Path.GetRelativePath(rootPath, filePath);
            await writer.WriteLineAsync($"=== FILE: {relativePath} ===");
            await writer.WriteLineAsync();

            try
            {
                var fileInfo = new FileInfo(filePath);
                
                if (fileInfo.Length > maxFileSize)
                {
                    _logger.LogDebug("File {FilePath} too large: {FileSize} bytes (limit: {MaxSize})", 
                                   relativePath, fileInfo.Length, maxFileSize);
                    await writer.WriteLineAsync($"[FILE TOO LARGE: {fileInfo.Length:N0} bytes, limit is {maxFileSize:N0} bytes]");
                }
                else
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    await writer.WriteLineAsync(content);
                    _logger.LogTrace("Successfully wrote content for file {FilePath} ({FileSize} bytes)", 
                                   relativePath, fileInfo.Length);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied to file {FilePath}", relativePath);
                await writer.WriteLineAsync("[ERROR: Access denied]");
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "IO error reading file {FilePath}", relativePath);
                await writer.WriteLineAsync($"[ERROR: File in use or locked - {ex.Message}]");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error reading file {FilePath}", relativePath);
                await writer.WriteLineAsync($"[ERROR READING FILE: {ex.Message}]");
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync("=== END OF FILE ===");
            await writer.WriteLineAsync();
        }
    }
}
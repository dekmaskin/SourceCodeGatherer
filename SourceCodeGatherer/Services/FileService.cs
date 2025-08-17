using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SourceCodeGatherer.Models;

namespace SourceCodeGatherer.Services
{
    /// <summary>
    /// Service for handling file operations with enhanced filtering and performance.
    /// </summary>
    public class FileService : IFileService
    {
        private readonly HashSet<string> _textExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetFileExtensionsAsync(string rootPath, IEnumerable<string> excludedDirectories = null)
        {
            return await Task.Run(() =>
            {
                var excludedDirs = excludedDirectories?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
                
                return GetFilteredFiles(rootPath, excludedDirs, null, long.MaxValue)
                    .Select(f => Path.GetExtension(f).ToLower())
                    .Where(ext => !string.IsNullOrWhiteSpace(ext) && IsTextFile(ext))
                    .Distinct()
                    .OrderBy(ext => ext);
            });
        }

        /// <inheritdoc/>
        public async Task ExportFilesAsync(string rootPath, string outputPath, IEnumerable<string> selectedExtensions, 
            AppSettings settings = null, IProgress<ExportProgress> progress = null)
        {
            settings ??= new AppSettings();
            
            if (settings.UseStreaming)
            {
                await ExportFilesStreamAsync(rootPath, outputPath, selectedExtensions, settings, progress);
            }
            else
            {
                var content = await ExportFilesToStringAsync(rootPath, selectedExtensions, settings, progress);
                await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8);
            }
        }

        /// <inheritdoc/>
        public async Task<string> ExportFilesToStringAsync(string rootPath, IEnumerable<string> selectedExtensions,
            AppSettings settings = null, IProgress<ExportProgress> progress = null)
        {
            settings ??= new AppSettings();
            var extensionSet = new HashSet<string>(selectedExtensions, StringComparer.OrdinalIgnoreCase);

            return await Task.Run(async () =>
            {
                using var writer = new StringWriter();
                var files = GetFilteredFiles(rootPath, settings.ExcludedDirectories, extensionSet, settings.MaxFileSizeBytes).ToList();
                
                var progressInfo = new ExportProgress { TotalFiles = files.Count };
                
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
                
                return writer.ToString();
            });
        }

        /// <inheritdoc/>
        public async Task<ExportStatistics> GetExportStatisticsAsync(string rootPath, IEnumerable<string> selectedExtensions,
            AppSettings settings = null)
        {
            settings ??= new AppSettings();
            var extensionSet = new HashSet<string>(selectedExtensions, StringComparer.OrdinalIgnoreCase);

            return await Task.Run(() =>
            {
                var allFiles = GetFilteredFiles(rootPath, settings.ExcludedDirectories, extensionSet, long.MaxValue).ToList();
                var validFiles = new List<string>();
                var skippedFiles = 0;
                long totalSize = 0;
                int estimatedLines = 0;

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
                        }
                    }
                    catch
                    {
                        skippedFiles++;
                    }
                }

                return new ExportStatistics
                {
                    TotalFiles = validFiles.Count,
                    TotalSizeBytes = totalSize,
                    SkippedFiles = skippedFiles,
                    EstimatedLines = estimatedLines
                };
            });
        }

        /// <inheritdoc/>
        public bool IsTextFile(string extension)
        {
            return _textExtensions.Contains(extension);
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
            var files = GetFilteredFiles(rootPath, settings.ExcludedDirectories, extensionSet, settings.MaxFileSizeBytes).ToList();
            
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
            HashSet<string> extensionSet, long maxFileSize)
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

                    // Check extension if specified
                    if (extensionSet != null && !extensionSet.Contains(Path.GetExtension(file)))
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
        private static async Task WriteFileContentAsync(TextWriter writer, string rootPath, string filePath, long maxFileSize)
        {
            var relativePath = Path.GetRelativePath(rootPath, filePath);
            await writer.WriteLineAsync($"=== FILE: {relativePath} ===");
            await writer.WriteLineAsync();

            try
            {
                var fileInfo = new FileInfo(filePath);
                
                if (fileInfo.Length > maxFileSize)
                {
                    await writer.WriteLineAsync($"[FILE TOO LARGE: {fileInfo.Length:N0} bytes, limit is {maxFileSize:N0} bytes]");
                }
                else
                {
                    var content = await File.ReadAllTextAsync(filePath);
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
    }
}
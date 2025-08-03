using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceCodeGatherer.Services
{
    /// <summary>
    /// Service for handling file operations.
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
            ".razor", ".fs", ".vb", ".vbs", ".asmx", ".aspx", ".jsp", ".jspx"
        };

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetFileExtensionsAsync(string rootPath)
        {
            return await Task.Run(() =>
            {
                return Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
                    .Select(f => Path.GetExtension(f).ToLower())
                    .Where(ext => !string.IsNullOrWhiteSpace(ext) && IsTextFile(ext))
                    .Distinct()
                    .OrderBy(ext => ext);
            });
        }

        /// <inheritdoc/>
        public async Task ExportFilesAsync(string rootPath, string outputPath, IEnumerable<string> selectedExtensions)
        {
            var content = await ExportFilesToStringAsync(rootPath, selectedExtensions);
            await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8);
        }

        /// <inheritdoc/>
        public async Task<string> ExportFilesToStringAsync(string rootPath, IEnumerable<string> selectedExtensions)
        {
            var extensionSet = new HashSet<string>(selectedExtensions, StringComparer.OrdinalIgnoreCase);

            return await Task.Run(() =>
            {
                using (var writer = new StringWriter())
                {
                    var files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => extensionSet.Contains(Path.GetExtension(f)))
                        .OrderBy(f => f);

                    foreach (var file in files)
                    {
                        WriteFileContent(writer, rootPath, file);
                    }

                    return writer.ToString();
                }
            });
        }

        /// <inheritdoc/>
        public bool IsTextFile(string extension)
        {
            return _textExtensions.Contains(extension);
        }

        /// <summary>
        /// Writes file content to the output stream.
        /// </summary>
        /// <param name="writer">The text writer.</param>
        /// <param name="rootPath">The root directory path.</param>
        /// <param name="filePath">The file path to write.</param>
        private static void WriteFileContent(TextWriter writer, string rootPath, string filePath)
        {
            var relativePath = Path.GetRelativePath(rootPath, filePath);
            writer.WriteLine($"=== FILE: {relativePath} ===");
            writer.WriteLine();

            try
            {
                var content = File.ReadAllText(filePath);
                writer.WriteLine(content);
            }
            catch (Exception ex)
            {
                writer.WriteLine($"[ERROR READING FILE: {ex.Message}]");
            }

            writer.WriteLine();
            writer.WriteLine("=== END OF FILE ===");
        }
    }
}
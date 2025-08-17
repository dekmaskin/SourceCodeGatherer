using System.Collections.Generic;

namespace SourceCodeGatherer.Models
{
    /// <summary>
    /// Application settings for persistence across sessions.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the last used root path.
        /// </summary>
        public string LastRootPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last used output path.
        /// </summary>
        public string LastOutputPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of preferred file extensions.
        /// </summary>
        public List<string> PreferredExtensions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of excluded directory patterns.
        /// </summary>
        public List<string> ExcludedDirectories { get; set; } = new List<string>
        {
            ".git", ".vs", ".vscode", "node_modules", "bin", "obj", "packages",
            ".nuget", "target", "build", "dist", ".next", ".svelte-kit"
        };

        /// <summary>
        /// Gets or sets the list of accepted file formats/extensions.
        /// </summary>
        public List<string> AcceptedFileFormats { get; set; } = new List<string>
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
        /// Gets or sets the maximum file size in bytes (default 10MB).
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        /// Gets or sets the maximum file size in KB for UI binding.
        /// </summary>
        public double MaxFileSizeKB
        {
            get => MaxFileSizeBytes / 1024.0;
            set => MaxFileSizeBytes = (long)(value * 1024);
        }

        /// <summary>
        /// Gets or sets the list of recent root paths.
        /// </summary>
        public List<string> RecentPaths { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether to use streaming for large exports.
        /// </summary>
        public bool UseStreaming { get; set; } = true;

        /// <summary>
        /// Gets or sets the project-specific window settings.
        /// </summary>
        public Dictionary<string, ProjectWindowSettings> ProjectSettings { get; set; } = new Dictionary<string, ProjectWindowSettings>();
    }
}
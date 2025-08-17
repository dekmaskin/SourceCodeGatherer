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
        /// Gets or sets the maximum file size in bytes (default 10MB).
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        /// Gets or sets the list of recent root paths.
        /// </summary>
        public List<string> RecentPaths { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether to use streaming for large exports.
        /// </summary>
        public bool UseStreaming { get; set; } = true;
    }
}
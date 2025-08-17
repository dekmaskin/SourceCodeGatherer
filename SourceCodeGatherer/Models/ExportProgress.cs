namespace SourceCodeGatherer.Models
{
    /// <summary>
    /// Represents progress information for export operations.
    /// </summary>
    public class ExportProgress
    {
        /// <summary>
        /// Gets or sets the current file being processed.
        /// </summary>
        public string CurrentFile { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of files processed.
        /// </summary>
        public int FilesProcessed { get; set; }

        /// <summary>
        /// Gets or sets the total number of files to process.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Gets or sets the percentage complete (0-100).
        /// </summary>
        public int PercentComplete => TotalFiles > 0 ? (FilesProcessed * 100) / TotalFiles : 0;

        /// <summary>
        /// Gets or sets the total bytes processed.
        /// </summary>
        public long BytesProcessed { get; set; }

        /// <summary>
        /// Gets or sets any error message.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
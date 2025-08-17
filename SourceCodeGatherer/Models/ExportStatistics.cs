namespace SourceCodeGatherer.Models
{
    /// <summary>
    /// Represents statistics about an export operation.
    /// </summary>
    public class ExportStatistics
    {
        /// <summary>
        /// Gets or sets the total number of files that will be exported.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Gets or sets the total size in bytes of all files to be exported.
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the number of files that were skipped due to size limits.
        /// </summary>
        public int SkippedFiles { get; set; }

        /// <summary>
        /// Gets or sets the estimated total lines of code.
        /// </summary>
        public int EstimatedLines { get; set; }

        /// <summary>
        /// Gets the total size in a human-readable format.
        /// </summary>
        public string FormattedSize
        {
            get
            {
                if (TotalSizeBytes < 1024)
                    return $"{TotalSizeBytes} B";
                if (TotalSizeBytes < 1024 * 1024)
                    return $"{TotalSizeBytes / 1024.0:F1} KB";
                if (TotalSizeBytes < 1024 * 1024 * 1024)
                    return $"{TotalSizeBytes / (1024.0 * 1024.0):F1} MB";
                return $"{TotalSizeBytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }
    }
}
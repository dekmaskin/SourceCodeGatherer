using System.Collections.Generic;

namespace SourceCodeGatherer.Models
{
    /// <summary>
    /// Window settings specific to a project/root path.
    /// </summary>
    public class ProjectWindowSettings
    {
        /// <summary>
        /// Gets or sets the project root path (used as identifier).
        /// </summary>
        public string ProjectPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the window width.
        /// </summary>
        public double WindowWidth { get; set; } = 800;

        /// <summary>
        /// Gets or sets the window height.
        /// </summary>
        public double WindowHeight { get; set; } = 650;

        /// <summary>
        /// Gets or sets the window left position.
        /// </summary>
        public double WindowLeft { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the window top position.
        /// </summary>
        public double WindowTop { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the window state (Normal, Minimized, Maximized).
        /// </summary>
        public string WindowState { get; set; } = "Normal";

        /// <summary>
        /// Gets or sets the selected file extensions for this project.
        /// </summary>
        public List<string> SelectedExtensions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the maximum file size in KB for this project.
        /// </summary>
        public double MaxFileSizeKB { get; set; } = 10240;

        /// <summary>
        /// Gets or sets the last output path for this project.
        /// </summary>
        public string LastOutputPath { get; set; } = string.Empty;
    }
}
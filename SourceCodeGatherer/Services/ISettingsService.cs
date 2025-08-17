using System.Threading.Tasks;
using SourceCodeGatherer.Models;

namespace SourceCodeGatherer.Services
{
    /// <summary>
    /// Interface for application settings service.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Loads application settings.
        /// </summary>
        /// <returns>The loaded settings or default settings if none exist.</returns>
        Task<AppSettings> LoadSettingsAsync();

        /// <summary>
        /// Saves application settings.
        /// </summary>
        /// <param name="settings">The settings to save.</param>
        Task SaveSettingsAsync(AppSettings settings);

        /// <summary>
        /// Adds a path to recent paths list.
        /// </summary>
        /// <param name="path">The path to add.</param>
        Task AddRecentPathAsync(string path);

        /// <summary>
        /// Gets project-specific window settings for a given project path.
        /// </summary>
        /// <param name="projectPath">The project root path.</param>
        /// <returns>The project window settings or default settings if none exist.</returns>
        Task<ProjectWindowSettings> GetProjectWindowSettingsAsync(string projectPath);

        /// <summary>
        /// Saves project-specific window settings.
        /// </summary>
        /// <param name="projectPath">The project root path.</param>
        /// <param name="windowSettings">The window settings to save.</param>
        Task SaveProjectWindowSettingsAsync(string projectPath, ProjectWindowSettings windowSettings);
    }
}
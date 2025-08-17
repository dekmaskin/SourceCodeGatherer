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
    }
}
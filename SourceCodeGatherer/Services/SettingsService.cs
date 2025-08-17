using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SourceCodeGatherer.Models;

namespace SourceCodeGatherer.Services
{
    /// <summary>
    /// Service for handling application settings persistence.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsPath;
        private AppSettings _cachedSettings;

        /// <summary>
        /// Initializes a new instance of the SettingsService class.
        /// </summary>
        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "SourceCodeGatherer");
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");
        }

        /// <inheritdoc/>
        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = await File.ReadAllTextAsync(_settingsPath);
                    _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _cachedSettings = new AppSettings();
                }
            }
            catch
            {
                _cachedSettings = new AppSettings();
            }

            return _cachedSettings;
        }

        /// <inheritdoc/>
        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(_settingsPath, json);
                _cachedSettings = settings;
            }
            catch
            {
                // Silently fail - settings are not critical
            }
        }

        /// <inheritdoc/>
        public async Task AddRecentPathAsync(string path)
        {
            var settings = await LoadSettingsAsync();
            
            // Remove if already exists
            settings.RecentPaths.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            
            // Add to beginning
            settings.RecentPaths.Insert(0, path);
            
            // Keep only last 10
            if (settings.RecentPaths.Count > 10)
            {
                settings.RecentPaths = settings.RecentPaths.Take(10).ToList();
            }

            await SaveSettingsAsync(settings);
        }
    }
}
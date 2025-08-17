using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SourceCodeGatherer.Models;

namespace SourceCodeGatherer.Services
{
    /// <summary>
    /// Service for handling application settings persistence.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsPath;
        private readonly ILogger<SettingsService> _logger;
        private AppSettings _cachedSettings;

        /// <summary>
        /// Initializes a new instance of the SettingsService class.
        /// </summary>
        public SettingsService()
        {
            _logger = App.LoggingService?.GetLogger<SettingsService>() ?? 
                     Microsoft.Extensions.Logging.Abstractions.NullLogger<SettingsService>.Instance;

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "SourceCodeGatherer");
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");
            
            _logger.LogDebug("SettingsService initialized. Settings path: {SettingsPath}", _settingsPath);
        }

        /// <inheritdoc/>
        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (_cachedSettings != null)
            {
                _logger.LogTrace("Returning cached settings");
                return _cachedSettings;
            }

            _logger.LogDebug("Loading settings from file: {SettingsPath}", _settingsPath);

            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = await File.ReadAllTextAsync(_settingsPath);
                    _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    
                    _logger.LogInformation("Settings loaded successfully. Recent paths: {RecentPathCount}, Project settings: {ProjectSettingsCount}",
                                         _cachedSettings.RecentPaths?.Count ?? 0, _cachedSettings.ProjectSettings?.Count ?? 0);
                }
                else
                {
                    _logger.LogInformation("Settings file not found, creating default settings");
                    _cachedSettings = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings, using defaults");
                _cachedSettings = new AppSettings();
            }

            return _cachedSettings;
        }

        /// <inheritdoc/>
        public async Task SaveSettingsAsync(AppSettings settings)
        {
            _logger.LogDebug("Saving settings to file: {SettingsPath}", _settingsPath);

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(_settingsPath, json);
                _cachedSettings = settings;
                
                _logger.LogDebug("Settings saved successfully. File size: {FileSize} bytes", json.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings to file: {SettingsPath}", _settingsPath);
            }
        }

        /// <inheritdoc/>
        public async Task AddRecentPathAsync(string path)
        {
            _logger.LogDebug("Adding recent path: {Path}", path);

            var settings = await LoadSettingsAsync();
            var originalCount = settings.RecentPaths.Count;
            
            // Remove if already exists
            settings.RecentPaths.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            
            // Add to beginning
            settings.RecentPaths.Insert(0, path);
            
            // Keep only last 10
            if (settings.RecentPaths.Count > 10)
            {
                settings.RecentPaths = settings.RecentPaths.Take(10).ToList();
            }

            _logger.LogDebug("Recent paths updated. Count: {OriginalCount} -> {NewCount}", originalCount, settings.RecentPaths.Count);

            await SaveSettingsAsync(settings);
        }

        /// <inheritdoc/>
        public async Task<ProjectWindowSettings> GetProjectWindowSettingsAsync(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                _logger.LogWarning("GetProjectWindowSettingsAsync called with empty project path");
                return new ProjectWindowSettings();
            }

            var normalizedPath = Path.GetFullPath(projectPath).ToLowerInvariant();
            _logger.LogDebug("Getting project window settings for: {ProjectPath}", normalizedPath);

            var settings = await LoadSettingsAsync();
            
            if (settings.ProjectSettings.TryGetValue(normalizedPath, out var projectSettings))
            {
                _logger.LogDebug("Found existing project settings with {ExtensionCount} selected extensions",
                               projectSettings.SelectedExtensions?.Count ?? 0);
                return projectSettings;
            }

            _logger.LogDebug("No existing project settings found, returning defaults");
            return new ProjectWindowSettings { ProjectPath = normalizedPath };
        }

        /// <inheritdoc/>
        public async Task SaveProjectWindowSettingsAsync(string projectPath, ProjectWindowSettings windowSettings)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                _logger.LogWarning("SaveProjectWindowSettingsAsync called with empty project path");
                return;
            }

            var normalizedPath = Path.GetFullPath(projectPath).ToLowerInvariant();
            _logger.LogDebug("Saving project window settings for: {ProjectPath}. Extensions: {ExtensionCount}, Window size: {Width}x{Height}",
                           normalizedPath, windowSettings.SelectedExtensions?.Count ?? 0, windowSettings.WindowWidth, windowSettings.WindowHeight);

            var settings = await LoadSettingsAsync();
            
            windowSettings.ProjectPath = normalizedPath;
            settings.ProjectSettings[normalizedPath] = windowSettings;

            await SaveSettingsAsync(settings);
            _logger.LogDebug("Project window settings saved successfully");
        }
    }
}
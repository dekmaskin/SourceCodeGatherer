using System;
using System.Linq;
using System.Windows.Input;
using SourceCodeGatherer.Commands;
using SourceCodeGatherer.Models;
using SourceCodeGatherer.Services;

namespace SourceCodeGatherer.ViewModels
{
    /// <summary>
    /// ViewModel for the settings window.
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly AppSettings _originalSettings;
        private string _excludedDirectoriesText;
        private string _acceptedFileFormatsText;
        private bool _useStreaming;

        /// <summary>
        /// Initializes a new instance of the SettingsViewModel class.
        /// </summary>
        /// <param name="settingsService">The settings service.</param>
        /// <param name="currentSettings">The current application settings.</param>
        public SettingsViewModel(ISettingsService settingsService, AppSettings currentSettings)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _originalSettings = currentSettings ?? throw new ArgumentNullException(nameof(currentSettings));

            // Initialize properties from current settings
            ExcludedDirectoriesText = string.Join(Environment.NewLine, _originalSettings.ExcludedDirectories);
            AcceptedFileFormatsText = string.Join(Environment.NewLine, _originalSettings.AcceptedFileFormats);
            UseStreaming = _originalSettings.UseStreaming;

            SaveCommand = new RelayCommand(ExecuteSave);
        }

        #region Properties

        /// <summary>
        /// Gets or sets the excluded directories text.
        /// </summary>
        public string ExcludedDirectoriesText
        {
            get => _excludedDirectoriesText;
            set => SetProperty(ref _excludedDirectoriesText, value);
        }

        /// <summary>
        /// Gets or sets the accepted file formats text.
        /// </summary>
        public string AcceptedFileFormatsText
        {
            get => _acceptedFileFormatsText;
            set => SetProperty(ref _acceptedFileFormatsText, value);
        }



        /// <summary>
        /// Gets or sets whether to use streaming.
        /// </summary>
        public bool UseStreaming
        {
            get => _useStreaming;
            set => SetProperty(ref _useStreaming, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the save command.
        /// </summary>
        public ICommand SaveCommand { get; }

        #endregion

        #region Private Methods

        private async void ExecuteSave()
        {
            try
            {
                // Update the original settings object
                _originalSettings.ExcludedDirectories = ExcludedDirectoriesText
                    .Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                _originalSettings.AcceptedFileFormats = AcceptedFileFormatsText
                    .Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                _originalSettings.UseStreaming = UseStreaming;

                await _settingsService.SaveSettingsAsync(_originalSettings);
            }
            catch
            {
                // Silently fail - settings are not critical
            }
        }

        #endregion
    }
}
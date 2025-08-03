using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SourceCodeGatherer.Models
{
    /// <summary>
    /// Represents a file extension item with checkbox state.
    /// </summary>
    public class FileExtensionItem : INotifyPropertyChanged
    {
        private string _extension;
        private bool _isChecked;

        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        public string Extension
        {
            get => _extension;
            set
            {
                _extension = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether this extension is selected.
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
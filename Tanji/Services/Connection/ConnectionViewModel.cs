using System.Windows.Input;

using Microsoft.Win32;

using Tanji.Helpers;

namespace Tanji.Services.Connection
{
    public class ConnectionViewModel : ObservableObject
    {
        private readonly OpenFileDialog _openDialog;

        private string _state = Constants.STANDING_BY;
        public string State
        {
            get { return _state; }
            set
            {
                _state = value;
                RaiseOnPropertyChanged();

                IsConnecting =
                    (_state != Constants.STANDING_BY);
            }
        }

        private bool _isConnecting = false;
        public bool IsConnecting
        {
            get { return _isConnecting; }
            set
            {
                _isConnecting = value;
                RaiseOnPropertyChanged();
            }
        }

        private string _customClientPath = string.Empty;
        public string CustomClientPath
        {
            get { return _customClientPath; }
            set
            {
                _customClientPath = value;
                RaiseOnPropertyChanged();
            }
        }

        public ICommand CancelCommand { get; }
        public ICommand BrowseCommand { get; }
        public ICommand ConnectCommand { get; }

        public ConnectionViewModel()
        {
            _openDialog = new OpenFileDialog();
            _openDialog.Title = "Tanji - Select Custom Client";
            _openDialog.Filter = "Shockwave Flash File (*.swf)|*.swf";

            BrowseCommand = new RelayCommand(AlwaysTrue, Browse);
            CancelCommand = new RelayCommand(AlwaysTrue, Cancel);
            ConnectCommand = new RelayCommand(AlwaysTrue, Connect);
        }

        private void Browse(object obj)
        {
            _openDialog.FileName = string.Empty;
            if (_openDialog.ShowDialog() == true)
            {
                CustomClientPath = _openDialog.FileName;
            }
        }
        private void Cancel(object obj)
        {
            State = Constants.STANDING_BY;
        }
        private void Connect(object obj)
        {
            State = Constants.INTERCEPTING_GAME_DATA;
        }
    }
}
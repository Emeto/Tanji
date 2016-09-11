using System;
using System.Reflection;

using Tanji.Helpers;
using Tanji.Services;
using Tanji.Windows.Logger;

namespace Tanji.Windows.Main
{
    public class MainViewModel : ObservableObject, IHaltable
    {
        private readonly LoggerView _loggerView;

        private string _title = "Tanji - Disconnected";
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                RaiseOnPropertyChanged();
            }
        }

        public Version LocalVersion { get; }

        public MainViewModel()
        {
            App.Haltables.Add(this);

            _loggerView = new LoggerView();

            LocalVersion = Assembly
                .GetExecutingAssembly().GetName().Version;
        }

        #region IHaltable Implementation
        public void Halt()
        {
            Title = "Tanji - Disconnected";
            _loggerView.Hide();
        }
        public void Restore()
        {
            Title = $"Tanji - Connected[{App.Interceptor.RemoteEndPoint}]";
            _loggerView.Show();
        }
        #endregion
    }
}
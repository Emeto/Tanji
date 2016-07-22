using System;
using System.Reflection;

using Tanji.Helpers;

namespace Tanji.Windows.Main
{
    public class MainViewModel : ObservableObject
    {
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
            LocalVersion = Assembly
                .GetExecutingAssembly().GetName().Version;
        }
    }
}
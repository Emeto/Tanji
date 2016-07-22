using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Sulakore.Habbo.Web;
using Sulakore.Communication;

namespace Tanji
{
    public partial class App : Application, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        protected void RaiseOnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            OnPropertyChanged(
                new PropertyChangedEventArgs(propertyName));
        }

        private int _schedulesCount = 0;
        public int SchedulesCount
        {
            get { return _schedulesCount; }
            set
            {
                _schedulesCount = value;
                RaiseOnPropertyChanged();
            }
        }

        private int _activeSchedulesCount = 0;
        public int ActiveSchedulesCount
        {
            get { return _activeSchedulesCount; }
            set
            {
                _activeSchedulesCount = value;
                RaiseOnPropertyChanged();
            }
        }

        private int _filtersCount = 0;
        public int FiltersCount
        {
            get { return _filtersCount; }
            set
            {
                _filtersCount = value;
                RaiseOnPropertyChanged();
            }
        }

        private int _activeFiltersCount = 0;
        public int ActiveFiltersCount
        {
            get { return _activeFiltersCount; }
            set
            {
                _activeFiltersCount = value;
                RaiseOnPropertyChanged();
            }
        }

        private int _activeExtensionsCount;
        public int ActiveExtensionsCount
        {
            get { return _activeExtensionsCount; }
            set
            {
                _activeExtensionsCount = value;
                RaiseOnPropertyChanged();
            }
        }

        private int _extensionsCount;
        public int ExtensionsCount
        {
            get { return _extensionsCount; }
            set
            {
                _extensionsCount = value;
                RaiseOnPropertyChanged();
            }
        }

        public static HGameData GameData { get; }
        public static HConnection Connection { get; }
        public static App Instance { get; private set; }

        static App()
        {
            GameData = new HGameData();
            Connection = new HConnection();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Instance = this;
            base.OnStartup(e);
        }
    }
}
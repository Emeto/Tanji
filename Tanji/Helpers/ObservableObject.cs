using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tanji.Helpers
{
    public class ObservableObject : INotifyPropertyChanged
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

        public bool AlwaysTrue(object obj) => true;
        public bool AlwaysFalse(object obj) => false;
    }
}
using Tanji.Helpers;

namespace Tanji.Services.Injection.Constructer
{
    public class ConstructerViewModel : ObservableObject
    {
        private ushort _header;
        public ushort Header
        {
            get { return _header; }
            set
            {
                _header = value;
                RaiseOnPropertyChanged();
            }
        }
    }
}
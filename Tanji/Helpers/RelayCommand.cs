using System;
using System.Windows.Input;

namespace Tanji.Helpers
{
    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Predicate<object> canExecute, Action<object> execute)
            : base(canExecute, execute)
        { }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Predicate<T> canExecute, Action<T> execute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }
        public bool CanExecute(object parameter)
        {
            return _canExecute((T)parameter);
        }
    }
}
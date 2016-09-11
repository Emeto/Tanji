using System;
using System.Windows.Input;

namespace Tanji.Helpers
{
    public class Command : ICommand
    {
        protected readonly Action<object> _execute;
        protected readonly Predicate<object> _canExecute;

        private event EventHandler _canExecuteChanged;
        public event EventHandler CanExecuteChanged
        {
            add
            {
                _canExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                _canExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        public bool IsInverse { get; set; }

        protected Command(Predicate<object> canExecute, bool isInverse)
        {
            _canExecute = canExecute;

            IsInverse = isInverse;
        }
        public Command(Predicate<object> canExecute, Action<object> execute)
            : this(canExecute, execute, false)
        { }
        public Command(Predicate<object> canExecute, Action<object> execute, bool isInverse)
            : this(canExecute, isInverse)
        {
            _execute = execute;
        }

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        public bool CanExecute(object parameter)
        {
            return (IsInverse !=
                _canExecute(parameter));
        }
        public virtual void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
using System;
using System.Windows.Input;
using System.Threading.Tasks;

namespace Tanji.Helpers
{
    public class AsyncCommand : Command
    {
        private readonly Func<object, Task> _executeAsync;

        public AsyncCommand(Predicate<object> canExecute, Func<object, Task> executeAsync)
            : this(canExecute, executeAsync, false)
        { }
        public AsyncCommand(Predicate<object> canExecute, Func<object, Task> executeAsync, bool isInverse)
            : base(canExecute, isInverse)
        {
            _executeAsync = executeAsync;
        }

        public override async void Execute(object parameter)
        {
            await _executeAsync(parameter);
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
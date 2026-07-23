using System;
using System.Windows.Input;

namespace NinOS.UI.Common
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _can_execute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? can_execute = null)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            _execute = execute;
            _can_execute = can_execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _can_execute == null || _can_execute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
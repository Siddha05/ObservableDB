using System;
using System.Windows.Input;

namespace ObservableDB.Model
{
    public class RelayCommand : ICommand
    {
        Action _execute;
        Func<bool> _canexec;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canexec == null ? true : _canexec();

        public void Execute(object parameter)
        {
            if (CanExecute(null)) _execute?.Invoke();
        }

        public RelayCommand(Action action, Func<bool> canexec = null)
        {
            _canexec = canexec; _execute = action;
        }
    }
}

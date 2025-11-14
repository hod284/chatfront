using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfApp1
{
    public class IRelayCommand : ICommand
    {
        private Action<object> _excute;
        private Func<object,bool> _canExecute;
        public event EventHandler CanExecuteChanged
        {
          add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public IRelayCommand( Action<object> action ,Func<object,bool> func)
        { 
          _excute =action;
            _canExecute = func;
        }
        // 버튼 눌리기전 실행
        public bool CanExecute(object parameter)
        {
            return _canExecute == null||   _canExecute(parameter);
        }
        // 버튼 눌렀을때 실행
        public void Execute(object parameter)
        {
             _excute(parameter);
        }
    }
}

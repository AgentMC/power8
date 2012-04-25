using System;
using System.Windows.Input;

namespace Power8
{
    public class MenuItemClickCommand : ICommand
    {
        public void Execute(object parameter)
        {
            var powerItem = parameter as PowerItem;
            if (powerItem == null || (powerItem.Parent == null && string.IsNullOrEmpty(powerItem.Argument)))
                return;
            powerItem.Invoke();
            BtnStck.Instance.Hide();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
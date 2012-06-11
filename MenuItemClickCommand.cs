using System;
using System.Windows.Input;
using Power8.Properties;

namespace Power8
{
    public class MenuItemClickCommand : ICommand
    {
        public void Execute(object parameter)
        {
            var powerItem = parameter as PowerItem;
            try
            {
                if (powerItem == null)
                    throw new Exception(Resources.Err_NoPiExtracted);
                powerItem.Invoke();
            }
            catch (Exception ex)
            {
                Util.DispatchCaughtException(ex);
            }
            BtnStck.Instance.Hide();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
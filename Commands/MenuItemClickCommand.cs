using System;
using System.Windows.Input;
using Power8.Properties;
using Power8.Views;

namespace Power8.Commands
{
    /// <summary>
    /// Standard PowerItem invoker, used thoughout all the menus 
    /// (not the context menus though). Parameter should be bound to data item.
    /// </summary>
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
#pragma warning disable 0067 //Unused event
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
    }
}
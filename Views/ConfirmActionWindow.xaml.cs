using System.Windows;
using Power8.Properties;

namespace Power8.Views
{
    /// <summary>
    /// Window to get user confirmation of PowerAction.
    /// </summary>
    public partial class ConfirmActionWindow
    {
        public ConfirmActionWindow(object action)
        {
            InitializeComponent();
            Title = NoLoc.Stg_AppShortName;
            CawConfirmButton.Content = action;
        }

        public ConfirmActionWindow(string confirmMessage, string confirmButtonText, string cancelButtonText)
            : this(confirmButtonText)
        {
            CawCancelButton.Content =  cancelButtonText;
            CawMessageTextBlock.Text = string.Format(confirmMessage);
        }

        private void ConfirmButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
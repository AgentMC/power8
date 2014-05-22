using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

            QuestionIcon.Source = GetQuestionIconSource();
            Title = NoLoc.Stg_AppShortName;
            CawConfirmButton.Content = action;
        }

        private static BitmapSource GetQuestionIconSource()
        {
            var icon = SystemIcons.Question;
            var bs = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            return bs;
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
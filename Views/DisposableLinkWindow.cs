using System.Windows;
using System.Windows.Documents;

namespace Power8.Views
{
    public class DisposableLinkWindow: DisposableWindow
    {
        /// <summary>
        /// Handles clicks on the Hyperlinks in this window
        /// </summary>
        protected void Navigate(object sender, RoutedEventArgs e)
        {
            Util.CreateProcess(((Hyperlink)sender).NavigateUri.AbsoluteUri);
        }
    }
}

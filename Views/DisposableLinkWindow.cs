using System;
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

        private static readonly string[] Names = //different P8 images
        {"", "Blue_", "Green_", "marine_", "Red_", "violet_", "yellow_"};
        private static readonly Random Rnd = new Random(); //used to get random image

        /// <summary>
        /// Returns string that can be used as ImageSource, and containing
        /// the random Power8 image.
        /// </summary>
        public string Logo
        {
            get
            {
                return "/Power8;component/Images/logo_alfa/Power8Logo7_" +
                       Names[Rnd.Next(Names.Length)] +
                       "alfa.png";
            }
        }

        /// <summary>
        /// Handles OK button click
        /// </summary>
        protected void SimpleClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Returns Uri to Rower8 online repo, taking it from NoLoc resource.
        /// </summary>
        public Uri RepoUri
        {
            get
            {
                return new Uri(Properties.NoLoc.Stg_Power8URI);
            }
        }
    }
}

using System;
using System.Windows;
using System.Windows.Documents;
using System.Diagnostics;

namespace Power8.Views
{
    /// <summary>
    /// The About window, inherits DisposableWindow
    /// </summary>
    public partial class Donate
    {
        public Donate()
        {
            Util.FpReset();
            InitializeComponent();
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
        /// Handles clicks on the Hyperlinks in this window
        /// </summary>
        private void Navigate(object sender, RoutedEventArgs e)
        {
            Util.CreateProcess(((Hyperlink) sender).NavigateUri.AbsoluteUri);
        }
        /// <summary>
        /// Handles OK button click
        /// </summary>
        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

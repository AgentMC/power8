using System;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Diagnostics;

namespace Power8.Views
{
    /// <summary>
    /// The About window, inherits DisposableWindow
    /// </summary>
    public partial class About
    {
        public About()
        {
            InitializeComponent();
        }

        private static readonly string[] Names = //different P8 images
            new[] {"", "Blue_", "Green_", "marine_", "Red_", "violet_", "yellow_"};
        private static readonly Random Rnd = new Random(); //used to get random image

        /// <summary>
        /// Gets the "Copyright" assembly description of currently running application
        /// </summary>
        public string CopyrightContent
        {
            get
            {
                return ((AssemblyCopyrightAttribute) (Assembly
                                                         .GetEntryAssembly()
                                                         .GetCustomAttributes(typeof (AssemblyCopyrightAttribute), true)
                                                         [0])).Copyright;
            }
        }
        /// <summary>
        /// Gets the string representation of version of currently running application
        /// </summary>
        public string VersionContent
        {
            get { return Assembly.GetEntryAssembly().GetName().Version.ToString(); }
        }
        /// <summary>
        /// If the Localizer url is available in localization, returns instance of 
        /// corresponding URI. Returns null otherwise.
        /// </summary>
        public Uri UriContent
        {
            get
            {
                return string.IsNullOrEmpty(Properties.Resources.Str_LocalizerUri)
                           ? null
                           : new Uri(Properties.Resources.Str_LocalizerUri);
            }
        }

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
            Process.Start(((Hyperlink) sender).NavigateUri.AbsoluteUri);
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

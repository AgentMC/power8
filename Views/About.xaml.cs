using System;
using System.Reflection;
using System.Windows;

namespace Power8.Views
{
    /// <summary>
    /// The About window, inherits DisposableWindow
    /// </summary>
    public partial class About
    {
        public About()
        {
            Util.FpReset();
            InitializeComponent();
        }

        private static readonly string[] Names = //different P8 images
            {"", "Blue_", "Green_", "marine_", "Red_", "violet_", "yellow_"};
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
            get { return Util.GetAppVersion().ToString(); }
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
        /// Handles OK button click
        /// </summary>
        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

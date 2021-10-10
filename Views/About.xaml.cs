using System;
using System.Reflection;

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

        /// <summary>
        /// Gets the "Copyright" assembly description of currently running application
        /// </summary>
        public string CopyrightContent
        {
            get
            {
                return ((AssemblyCopyrightAttribute)
                    (Assembly.GetEntryAssembly()
                             .GetCustomAttributes(typeof (AssemblyCopyrightAttribute), true)[0]))
                    .Copyright;
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
                return (!string.IsNullOrEmpty(Properties.Resources.Str_LocalizerUri)) 
                        && Uri.TryCreate(Properties.Resources.Str_LocalizerUri, UriKind.Absolute, out var u)
                           ? u
                           : null;
            }
        }

    }
}

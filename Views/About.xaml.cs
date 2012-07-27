using System;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Diagnostics;

namespace Power8.Views
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About
    {
        public About()
        {
            InitializeComponent();
        }

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

        public string VersionContent
        {
            get { return Assembly.GetEntryAssembly().GetName().Version.ToString(); }
        }

        public Uri UriContent
        {
            get
            {
                return string.IsNullOrEmpty(Properties.Resources.Str_LocalizerUri)
                           ? null
                           : new Uri(Properties.Resources.Str_LocalizerUri);
            }
        }

        private readonly string[] _names = new[] {"", "Blue_", "Green_", "marine_", "Red_", "violet_", "yellow_"};
        public string Logo
        {
            get
            {
                return "/Power8;component/Images/logo_alfa/Power8Logo7_" + 
                       _names[new Random().Next(_names.Length)] +
                       "alfa.png";
            }
        }

        private void Navigate(object sender, RoutedEventArgs e)
        {
            Process.Start(((Hyperlink) sender).NavigateUri.AbsoluteUri);
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

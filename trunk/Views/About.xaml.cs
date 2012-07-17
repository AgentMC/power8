using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Diagnostics;

namespace Power8.Views
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : IComponent
    {
        public About()
        {
            InitializeComponent();
        }

        ~About()
        {
            Dispose();
        }

        private bool _disposing;
        public void Dispose()
        {
#if DEBUG
            Debug.WriteLine("Dispose called for About Window");
#endif
            lock (this)
            {
                if (_disposing)
                    return;
                _disposing = true;
            }
            Util.Send(() =>
                          {
                              if(IsVisible)
                                  Close();
                              var handler = Disposed;
                              if (handler != null)
                                  handler(this, null);
                          });
        }
        public event EventHandler Disposed;

        public ISite Site { get; set; }








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
                return new Uri(string.IsNullOrEmpty(Properties.Resources.Str_LocalizerUri)
                                   ? Properties.NoLoc.Stg_Power8URI
                                   : Properties.Resources.Str_LocalizerUri);
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

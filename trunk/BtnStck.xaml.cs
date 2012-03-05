using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Power8
{
    /// <summary>
    /// Interaction logic for BtnStck.xaml
    /// </summary>
    public partial class BtnStck
    {
        private static BtnStck _instance;
        public static BtnStck Instance
        {
            get { return _instance ?? (_instance = new BtnStck()); }
            private set { _instance = value; }
        }

        public BtnStck()
        {
            InitializeComponent();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !MainWindow.ClosedW;
            if (e.Cancel)
                Hide();
            else
                Instance = null;
        }

        private void WindowDeactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            var hlpr = new WindowInteropHelper(this);
            HwndSource.FromHwnd(hlpr.Handle).CompositionTarget.BackgroundColor = Colors.Transparent;
            API.MakeGlass(hlpr.Handle);
            MinHeight = Height;
            MaxHeight = MinHeight;
            MinWidth = Width;
            MaxWidth = MinWidth;
        }
    }
}

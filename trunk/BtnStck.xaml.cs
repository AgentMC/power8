using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Power8
{
    /// <summary>
    /// Interaction logic for BtnStck.xaml
    /// </summary>
    public partial class BtnStck : Window
    {
        private static BtnStck _instance;
        public static BtnStck Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BtnStck();
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public BtnStck()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !MainWindow.ClosedW;
            if (e.Cancel)
                Hide();
            else
                Instance = null;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }
    }
}

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
    /// Interaction logic for ComputerList.xaml
    /// </summary>
    public partial class ComputerList : Window
    {
        public ComputerList()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var comp in NetManager.ComputersNearby)
            {
                listBox1.Items.Add(comp);
            }
        }
    }
}

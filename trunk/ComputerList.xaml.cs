using System.Windows;

namespace Power8
{
    /// <summary>
    /// Interaction logic for ComputerList.xaml
    /// </summary>
    public partial class ComputerList
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

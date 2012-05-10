using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

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
            DataContext = this;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var comp in NetManager.ComputersNearby)
            {
                listBox1.Items.Add(comp);
            }
        }

        public ImageSource IconEx
        {
            get { return PowerItemTree.NetworkRoot.Items[0]/*workgroup*/.Icon.SmallBitmap; }
        }

        private void CleanerBtnClick(object sender, RoutedEventArgs e)
        {
            textbox1.Text = "";
        }

        private void Textbox1TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var txt = textbox1.Text.ToUpper();
            listBox1.Items.Clear();
            foreach (var comp in NetManager.ComputersNearby.Where(comp => comp.Contains(txt)))
            {
                listBox1.Items.Add(comp);
            }
        }

        private void ListBox1MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", "\\\\ " + listBox1.SelectedItem);
        }
    }
}

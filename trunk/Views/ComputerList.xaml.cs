using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Power8.Views
{
    public partial class ComputerList
    {
        public ComputerList()
        {
            InitializeComponent();
            DataContext = this;
            foreach (var comp in NetManager.ComputersNearby)
                listBox1.Items.Add(comp);
        }

        public ImageSource IconEx
        {
            get { return PowerItemTree.NetworkRoot.Items[0]/*workgroup*/.Icon.SmallBitmap; }
        }

        private void CleanerBtnClick(object sender, RoutedEventArgs e)
        {
            CLTextbox.Text = string.Empty;
        }

        private void Textbox1TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var txt = CLTextbox.Text.ToUpper();
            listBox1.Items.Clear();
            foreach (var comp in NetManager.ComputersNearby.Where(comp => comp.Contains(txt)))
                listBox1.Items.Add(comp);
        }

        private void ListBox1MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Util.StartExplorer("\\\\" + listBox1.SelectedItem);
        }

    }
}

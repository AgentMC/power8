using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Power8.Views
{
    /// <summary>
    /// This class works as list and quick search among computers
    /// available on network, if the amount is more than ten.
    /// </summary>
    public partial class ComputerList
    {
        /// <summary>
        /// Constructor. Initialize and fill data
        /// </summary>
        public ComputerList()
        {
            Util.FpReset();
            InitializeComponent();
            DataContext = this;
            foreach (var comp in NetManager.ComputersNearby)
                listBox1.Items.Add(comp);
        }
        /// <summary>
        /// Bindable ImageSource for Window icon.
        /// Returns already extracted "Workgroup" icon
        /// </summary>
        public ImageSource IconEx
        {
            get { return PowerItemTree.NetworkRoot.Items[0]/*workgroup*/.Icon.SmallBitmap; }
        }
        /// <summary>
        /// Clear button handler. Clears text in search box.
        /// </summary>
        private void CleanerBtnClick(object sender, RoutedEventArgs e)
        {
            CLTextbox.Text = string.Empty;
        }
        /// <summary>
        /// Search box text changed handler. Filters the results to items that contain entered text.
        /// Case insensitive, and fast since all computers are stored UPPERCASE.
        /// </summary>
        private void Textbox1TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var txt = CLTextbox.Text.ToUpper();
            listBox1.Items.Clear();
            foreach (var comp in NetManager.ComputersNearby.Where(comp => comp.Contains(txt)))
                listBox1.Items.Add(comp);
        }
        /// <summary>
        /// Currently open the computer - is the only one available action.
        /// By doubleclick.
        /// </summary>
        private void ListBox1MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Util.StartExplorer("\\\\" + listBox1.SelectedItem);
        }

    }
}

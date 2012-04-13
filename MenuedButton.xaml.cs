using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Power8
{
    /// <summary>
    /// Interaction logic for MenuedButton.xaml
    /// </summary>
    public partial class MenuedButton:INotifyPropertyChanged
    {
        public MenuedButton()
        {
            InitializeComponent();
            ItemsList.DataContext = this;
            ItemsList.PlacementTarget = menuDropper;
            ItemsList.Placement = PlacementMode.Left;
        }

        private PowerItem _item;
        public PowerItem Item
        {
            get { return _item; }
            set
            {
                if (_item != value)
                {
                    _item = value;
                    OnPropertyChanged("Item");
                }
            }
        }

        private ContextMenu ItemsList
        {
            get { return ((ContextMenu) Resources["itemsList"]); }
        }

        private void DisplayMenu(object sender, RoutedEventArgs e)
        {
            ItemsList.IsOpen = true;
        }

        public BtnStck Mnu
        {
            get { return BtnStck.Instance; }
        }

        private void ContextMenuContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ((App)Application.Current).MenuDataContext = Util.ExtractRelatedPowerItem(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(property));
        }
    }
}

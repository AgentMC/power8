using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

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

        public BtnStck Mnu
        {
            get { return BtnStck.Instance; }
        }

        private void ContextMenuContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            App.Current.MenuDataContext = Util.ExtractRelatedPowerItem(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(property));
        }

        private void actionBtnClick(object sender, RoutedEventArgs e)
        {
            Item.Invoke();
        }
    }
}

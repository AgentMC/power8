using System.ComponentModel;
using System.Windows.Controls;

namespace Power8
{
    /// <summary>
    /// MenuedButton the button with image-dropdown. Represents PowerItem.
    /// </summary>
    public partial class MenuedButton:INotifyPropertyChanged
    {
        public MenuedButton()
        {
            InitializeComponent();
        }

        private PowerItem _item;
        /// <summary>
        /// The PowerItem represented by this menued button.
        /// This is not implemented as DependencyProperty since a lot of work 
        /// is done automatically in case of DP, so the designer can crash 
        /// from time to time. This doesn't happen with regular property.
        /// </summary>
        public PowerItem Item
        {
            get { return _item; }
            set
            {
                if (_item == value) 
                    return;
                _item = value;
                var handler = PropertyChanged;
                if (handler != null) 
                    handler(this, new PropertyChangedEventArgs("Item"));
            }
        }

        /// <summary>
        /// The context menu is over the drop-down PowerItems.
        /// So what we are doing is setting the data context for that menu 
        /// to the PowerItem clicked.
        /// </summary>
        private void ContextMenuContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            App.Current.MenuDataContext = Util.ExtractRelatedPowerItem(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

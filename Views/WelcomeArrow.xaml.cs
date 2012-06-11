using System.ComponentModel;

namespace Power8.Views
{
    public partial class WelcomeArrow : INotifyPropertyChanged
    {
        public WelcomeArrow()
        {
            InitializeComponent();
        }

        private double _rotation;
        public double Rotation 
        {
            get { return _rotation; } 
            set
            {
                _rotation = value;
                var h = PropertyChanged;
                if (h != null)
                    h(this, new PropertyChangedEventArgs("Rotation"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

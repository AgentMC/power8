using System.ComponentModel;

namespace Power8.Views
{
    /// <summary>
    /// Shows bouncing arrow pointed to Power8 button
    /// </summary>
    public partial class WelcomeArrow : INotifyPropertyChanged
    {
        /// <summary>
        /// .ctor
        /// </summary>
        public WelcomeArrow()
        {
            InitializeComponent();
        }

        private double _rotation;
        /// <summary>
        /// Gets or sets the angle the arrow is rotated, and thus the bouncing direction.
        /// </summary>
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

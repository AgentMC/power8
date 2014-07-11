using System;
using System.Windows;

namespace Power8.Views
{
    /// <summary>
    /// The About window, inherits DisposableWindow
    /// </summary>
    public partial class Donate
    {
        public Donate()
        {
            Util.FpReset();
            InitializeComponent();
        }

        private static readonly string[] Names = //different P8 images
            {"", "Blue_", "Green_", "marine_", "Red_", "violet_", "yellow_"};
        private static readonly Random Rnd = new Random(); //used to get random image

        /// <summary>
        /// Returns string that can be used as ImageSource, and containing
        /// the random Power8 image.
        /// </summary>
        public string Logo
        {
            get
            {
                return "/Power8;component/Images/logo_alfa/Power8Logo7_" + 
                       Names[Rnd.Next(Names.Length)] +
                       "alfa.png";
            }
        }

        /// <summary>
        /// Handles OK button click
        /// </summary>
        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

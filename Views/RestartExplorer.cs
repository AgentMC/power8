using System.Windows.Forms;
using Power8.Converters;

namespace Power8.Views
{
    /// <summary>
    /// The win32 window that is shown in case Explorer is dead 
    /// and we got to do something. Contains no logic except the localization.
    /// The only function of this is the question to user:
    /// Do you know what's happening or not? Because we don't like it!
    /// So the only thing whis window does is to return Yes or No :)
    /// </summary>
    public partial class RestartExplorer : Form
    {
        /// <summary>
        /// Constructor. Performs form init and loads the 
        /// localized strings for current UI culture.
        /// </summary>
        public RestartExplorer()
        {
            InitializeComponent();
            var c = new NameToResourceConverter();
            base.Text = c.Convert("RE_Title");
            label1.Text = c.Convert("RE_TopLabel");
            button1.Text = c.Convert("RE_YesButton");
            button2.Text = c.Convert("RE_NoButton");
  }
        /// <summary>
        /// ShowDialog() is overriden to guarantee the window will be 
        /// displayed foreground on all OS in the same way.
        /// </summary>
        new public void ShowDialog()
        {
            Show();
            BringToFront();
            Hide();
            base.ShowDialog();
        }
    }
}

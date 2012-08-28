using System.Windows.Forms;
using Power8.Converters;

namespace Power8.Views
{
    public partial class RestartExplorer : Form
    {
        public RestartExplorer()
        {
            InitializeComponent();
            var c = new NameToResourceConverter();
            base.Text = c.Convert("RE_Title");
            label1.Text = c.Convert("RE_TopLabel");
            button1.Text = c.Convert("RE_YesButton");
            button2.Text = c.Convert("RE_NoButton");
  }

        new public void ShowDialog()
        {
            Show();
            BringToFront();
            Hide();
            base.ShowDialog();
        }
    }
}

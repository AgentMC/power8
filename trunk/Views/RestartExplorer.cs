using System.Windows.Forms;

namespace Power8
{
    public partial class RestartExplorer : Form
    {
        public RestartExplorer()
        {
            InitializeComponent();
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

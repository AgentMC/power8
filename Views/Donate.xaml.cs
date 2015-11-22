using System;
using Power8.Properties;

namespace Power8.Views
{
    /// <summary>
    /// The Donate window, inherits DisposableLinkWindow. 
    /// </summary>
    public partial class Donate
    {
        public Donate()
        {
            Util.FpReset();
            InitializeComponent();
        }

        public Uri DonateUri
        {
            get { return new Uri(RepoUri, NoLoc.Stg_DonateUri); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Power8.DataProviders
{
    public class ImmersiveApp
    {
        public string ApplicationCompany { get; set; }
        public string ApplicationDescription { get; set; }
        //----
        public string ApplicationName { get; set; }
        public string ApplicationDisplayName { get; set; }

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(ApplicationDisplayName) ? ApplicationName : ApplicationDisplayName; }
        }

        public string AppUserModelID { get; set; }
        public string PackageId { get; set; }
        //------
        public string ApplicationPath { get; set; }
        public string File { get; set; }
        //------
        public string Logo
        {
            get { return Logos.Count > 0 ? Logos.First().Value : null; }
        }
        public Dictionary<string, string> Logos { get; set; }
        public Color Background { get; set; }
        public Color Foreground { get; set; }
        public Brush BackgroundBrush { get { return new SolidColorBrush(Background); } }
        public Brush ForegroundBrush { get { return new SolidColorBrush(Foreground); } }
        public string DisplayState { get; set; }

        private string[] blackList = { "Windows.ImmersiveControlPanel", "Microsoft.Windows.Cortana" };
        public bool IsSystemApp()
        {
            return DisplayState == "none" || blackList.Any(bl => PackageId.StartsWith(bl, StringComparison.OrdinalIgnoreCase));
        }
    }
}
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Management;
using System.Net;

namespace Power8
{
    static class NetManager
    {
        private static string _host, _wg;
        public static string Hostname
        {
            get { return _host ?? (_host = Dns.GetHostName()); }
        }

        public static string DomainOrWorkgroup
        {
            get
            {
                if (_wg == null)
                {
                    foreach (var obj in new ManagementObjectSearcher("select Domain from Win32_ComputerSystem").Get())
                        _wg = obj.GetPropertyValue("Domain").ToString();//only 1 item available
                }
                return _wg;
            }
        }

        private static readonly List<string> _computerNames = new List<string>();
        public static List<string> ComputersNearby
        {
            get
            {
                if (_computerNames.Count == 0)
                {
                    using (var workgroup = new DirectoryEntry("WinNT://" + DomainOrWorkgroup))
                    {
                        _computerNames.AddRange(workgroup.Children
                                                    .Cast<DirectoryEntry>()
                                                    .Where(e => e.SchemaClassName == "Computer")
                                                    .Select(e => e.Name));
                    }
                }
                return _computerNames;
            }
        }
    }
}

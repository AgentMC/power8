using System.Collections.Generic;
using System.Management;
using System.Net;
#if !DEBUG
using System.DirectoryServices;
using System.Linq;
#endif

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

        private static readonly List<string> ComputerNames = new List<string>();
        public static List<string> ComputersNearby
        {
            get
            {
                if (ComputerNames.Count == 0)
                {
#if DEBUG
                    for (int i = 0; i < 3000; i++)
                    {
                        ComputerNames.Add("COMPUTER" + i);
                    }
#else
                    using (var workgroup = new DirectoryEntry("WinNT://" + DomainOrWorkgroup))
                    {
                        ComputerNames.AddRange(workgroup.Children
                                                    .Cast<DirectoryEntry>()
                                                    .Where(e => e.SchemaClassName == "Computer")
                                                    .Select(e => e.Name.ToUpper()));
                    }
#endif
                }
                return ComputerNames;
            }
        }
    }
}

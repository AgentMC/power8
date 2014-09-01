using System;
using System.Collections.Generic;
using System.Management;
using System.Net;
#if !DEBUG
using System.DirectoryServices;
using System.Linq;
using System;
#endif
using Power8.Helpers;

namespace Power8
{
    /// <summary>
    /// Provides data related to the network activity PC might have
    /// </summary>
    static class NetManager
    {
        private static string _host, _wg;

        /// <summary>
        /// Gets the computer name
        /// </summary>
        public static string Hostname
        {
            get { return _host ?? (_host = Dns.GetHostName()); }
        }

        /// <summary>
        /// Gets the name of domain or workgroup
        /// </summary>
        public static string DomainOrWorkgroup
        {
            get
            {
                if (_wg == null)
                {
                    try
                    {
                        using (var m = new ManagementObjectSearcher("select Domain from Win32_ComputerSystem"))
                        using (var e = m.Get().GetEnumerator())
                        {
                            if (e.MoveNext())
                                _wg = e.Current.GetPropertyValue("Domain").ToString();
                        }
                    }
                    catch (Exception ex){Log.Raw(ex.Message);}
                    finally
                    {
                        if (string.IsNullOrWhiteSpace(_wg)) //modern
                            _wg = Environment.GetEnvironmentVariable("USERDOMAIN");
                        if (string.IsNullOrWhiteSpace(_wg)) //old
                            _wg = Environment.GetEnvironmentVariable("COMPUTERNAME");
                    }
                }
                return _wg;
            }
        }

        private static readonly List<string> ComputerNames = new List<string>();
        /// <summary>
        /// Returns the list of UPPERCASED computer names which are visible in domain or workgroup.
        /// In debug, returns "Computer1"..."Computer3000"
        /// </summary>
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
                    try
                    {
                        using (var workgroup = new DirectoryEntry("WinNT://" + DomainOrWorkgroup))
                        {
                            ComputerNames.AddRange(workgroup.Children
                                                       .Cast<DirectoryEntry>()
                                                       .Where(e => e.SchemaClassName == "Computer")
                                                       .Select(e => e.Name.ToUpper()));
                        }
                    }
                    catch (Exception ex)
                    {
                        Util.DispatchCaughtException(ex);
                        ComputerNames.Add(Hostname);
                    }
#endif
                }
                return ComputerNames;
            }
        }
    }
}

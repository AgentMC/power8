using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace Power8.DataProviders
{
    public static class AppUserModelIdProvider
    {
        private class AppxPackage
        {
            public string Identity;
            public Version Version;
            public AppxServer[] Servers;
        }

        private class AppxServer
        {
            public string AppUserModelId;
            public string[] ActivatableClasses;
        }

        private static Dictionary<string, AppxPackage> packages = new Dictionary<string, AppxPackage>();
        private static readonly object SyncObject = new object();
        
        public static string GetAppUserModelId(string identity, string appId)
        {
            if (!packages.ContainsKey(identity))
            {
                UpdateCache();
            }
            if (!packages.ContainsKey(identity))
            {
                return null;
            }
            var server = packages[identity].Servers.FirstOrDefault(s => s.ActivatableClasses.Contains(appId));
            return (server == null) ? null : server.AppUserModelId;
        }

        private static void UpdateCache()
        {
            lock (SyncObject)
            {
                const string package = @"ActivatableClasses\Package";
                var k = Registry.ClassesRoot.OpenSubKey(package);
                if (k == null)
                {
                    throw new Exception("Unable to open " + package + " key.");
                }
                packages.Clear();
                foreach (var subkeyName in k.GetSubKeyNames())
                {
                    AppxServer[] servers;
                    using (var packageKey = k.OpenSubKey(subkeyName + "\\Server"))
                    {
                        if(packageKey == null)
                            continue;
                        var serverNames = packageKey.GetSubKeyNames();
                        servers = new AppxServer[serverNames.Length];
                        for (int i = 0; i < serverNames.Length; i++)
                        {
                            using (var serverKey = packageKey.OpenSubKey(serverNames[i]))
                            {
                                if (serverKey == null)
                                    continue;
                                servers[i] =
                                    new AppxServer
                                    {
                                        AppUserModelId = (string) serverKey.GetValue("AppUserModelId"),
                                        ActivatableClasses = (string[])serverKey.GetValue("ActivatableClasses")
                                    };
                            }
                        }
                    }
                    var data = subkeyName.Split('_');
                    var pack = new AppxPackage
                               {
                                   Identity = data[0],
                                   Version = new Version(data[1]),
                                   Servers = servers
                               };
                    packages.Add(pack.Identity, pack);
                }
                k.Dispose();
            }
        }

    }
}

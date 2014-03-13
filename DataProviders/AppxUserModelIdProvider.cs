using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace Power8.DataProviders
{
    /// <summary>
    /// This class retrieves the AppUserModelId for the Win8 Metro-style apps
    /// </summary>
    public static class AppxUserModelIdProvider
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

        private static readonly Dictionary<string, AppxPackage> Packages = new Dictionary<string, AppxPackage>();
        private static readonly object SyncObject = new object();
        
        /// <summary>
        /// Returns AppUserModelId for AppX when available in the Registry, or null if cannot find one.
        /// </summary>
        /// <param name="identity">The Identity of AppX package, as listed in &lt;Identity&gt; tag in AppX Manifest.</param>
        /// <param name="appId">The ID of certain App of AppX package, as listed in corresponding
        /// &lt;Application&gt; tag of AppX Manifest</param>
        public static string GetAppUserModelId(string identity, string appId)
        {
            if (!Packages.ContainsKey(identity))
            {
                UpdateCache();
            }
            if (!Packages.ContainsKey(identity)) //still
            {
                return null;
            }
            var server = Packages[identity].Servers.FirstOrDefault(s => s.ActivatableClasses.Contains(appId));
            return (server == null) ? null : server.AppUserModelId;
        }

        private static void UpdateCache()
        {
            //the registry looks like:
            //[HKCR\ActivatableClasses\Package\<Identity_Version_Arch..._Token>\Server\<ServerId(ignored)>\]
            //AppUserModelId = User model Id (string)
            //ActivatableClasses = string[]
            //--------------------
            //Package 1<->n Server
            lock (SyncObject)
            {
                const string package = @"ActivatableClasses\Package";
                var k = Registry.ClassesRoot.OpenSubKey(package);
                if (k == null)
                {
                    throw new Exception("Unable to open " + package + " key.");
                }
                Packages.Clear();
                foreach (var subkeyName in k.GetSubKeyNames()) //for each package in registry...
                {
                    AppxServer[] servers;
                    using (var packageKey = k.OpenSubKey(subkeyName + "\\Server")) //if server is available (i.e. is Activatable)
                    {
                        if(packageKey == null)
                            continue;
                        var serverNames = packageKey.GetSubKeyNames();
                        servers = new AppxServer[serverNames.Length];
                        for (int i = 0; i < serverNames.Length; i++) //for each server in package...
                        {
                            using (var serverKey = packageKey.OpenSubKey(serverNames[i]))
                            {
                                if (serverKey == null)
                                    continue;
                                servers[i] =                    //Create AppxServer
                                    new AppxServer
                                    {
                                        AppUserModelId = (string) serverKey.GetValue("AppUserModelId"),
                                        ActivatableClasses = (string[])serverKey.GetValue("ActivatableClasses")
                                    };
                            }
                        }
                    }
                    var data = subkeyName.Split('_');
                    var pack = new AppxPackage                  //Create AppxPackage with generated Servers list
                               {
                                   Identity = data[0],
                                   Version = new Version(data[1]),
                                   Servers = servers
                               };
                    Packages.Add(pack.Identity, pack);          //Add to cache
                }
                k.Dispose();
            }
        }

    }
}

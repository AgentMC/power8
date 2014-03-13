using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using Power8.Helpers;

namespace Power8.DataProviders
{
    /// <summary>
    /// This class retrieves the AppUserModelId for the Win8 Metro-style apps
    /// </summary>
    public static class AppxUserModelIdProvider
    {
        private class AppxPackage
        {
            public string Identity, Token, Architecture;
            public Version Version;
            public AppxServer[] Servers;
            public override string ToString()
            {
                return string.Format("[{0}][{1}][{2}][{3}]: {4}", Identity, Version, Architecture, Token,
                                     string.Join(", ",
                                                 Servers.Select(s =>
                                                     string.Format("<{0}: {1}>", s.AppUserModelId, string.Join(", ", s.ActivatableClasses)))));
            }
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
            var logKey = identity + "|" + appId;
            if (!Packages.ContainsKey(identity))
            {
                Log.Raw("Cache miss", logKey);
                UpdateCache();
            }
            if (!Packages.ContainsKey(identity)) //still
            {
                Log.Raw("Double cache miss", logKey);
                return null;
            }
            var server = Packages[identity].Servers.FirstOrDefault(s => s.ActivatableClasses.Contains(appId));
            if (server == null)
            {
                Log.Raw("Cache double-miss on app", logKey);
                return null;
            }
            Log.Raw("Cache hit: " + server.AppUserModelId, logKey);
            return server.AppUserModelId;
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
                                   Servers = servers
                               };
                    if (data.Length >= 4)
                    {
                        pack.Version = new Version(data[1]);
                        pack.Architecture = data[2];
                        pack.Token = data[data.Length - 1];
                    }
                    Packages.Add(pack.Identity, pack);          //Add to cache
                    Log.Raw("Added package to cache: " + pack);
                }
                k.Dispose();
            }
        }

    }
}

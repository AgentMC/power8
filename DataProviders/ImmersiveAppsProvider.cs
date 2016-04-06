using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.Win32;
using Power8.Helpers;

namespace Power8.DataProviders
{
    public static class ImmersiveAppsProvider
    {
        public const string APPXMANIFEST_XML = "AppxManifest.xml";
        //Main entry point
        private static List<ImmersiveApp> _cache; 
        public static List<ImmersiveApp> GetAppsCache()
        {
            if (_cache == null)
                lock (APPXMANIFEST_XML)
                    if (_cache == null)
                        _cache = new List<ImmersiveApp>(GetApps());
            return _cache;
        }

        private static IEnumerable<ImmersiveApp> GetApps()
        {
            Log.Raw("Begin GetApps. OsIs 8+ = " + Util.OsIs.EightOrMore);
            //Only for Win8+
            if (!Util.OsIs.EightOrMore) yield break;

            //Step 1. Getting initial package deployment info: package ids and related deployment paths and display names
            var pathCache = GetDeploymentPathCache();
            if(pathCache == null || pathCache.Count == 0) yield break;

            //Step 2. Scanning classes repo for main executables and activatable clsids
            //Each package may have several "entry points" - servers, for now just read them all
            var serverCache = GetExecutableServerCache();
            if (serverCache == null || serverCache.Count == 0) yield break;

			//Step 3. Parse app manifest to get more visual information and corellate deployment and server caches
			XNamespace m1 = XNamespace.Get("http://schemas.microsoft.com/appx/2010/manifest"),
					   m2 = XNamespace.Get("http://schemas.microsoft.com/appx/2013/manifest"),
					   m3 = XNamespace.Get("http://schemas.microsoft.com/appx/manifest/foundation/windows10"),
					   m4 = XNamespace.Get("http://schemas.microsoft.com/appx/manifest/uap/windows10");

            foreach (var package in serverCache)
            {
				PathCacheItem pathCacheItem;
				if(!pathCache.TryGetValue(package.Key, out pathCacheItem))
				{
					Log.Raw("Skipping server", package.Key);
					continue;
				}
                Log.Raw("Loading server data", package.Key);
                var appxManifestPath = Path.Combine(pathCacheItem.RootPath, APPXMANIFEST_XML);
                if (!File.Exists(appxManifestPath)) continue;

                string company = null, logoTemplate = null;

                Log.Raw("Processing manifest " + appxManifestPath);
                var appManifest = XElement.Load(appxManifestPath);
                var props = appManifest.Element(m1 + "Properties");
                if (props != null)
                {
                    company = props.Element(m1 + "PublisherDisplayName").GetValueOrNull();
                    logoTemplate = props.Element(m1 + "Logo").GetValueOrNull();
                }
				// ReSharper disable once PossibleNullReferenceException
				var appDataItems = (appManifest.Element(m1 + "Applications") ?? appManifest.Element(m3 + "Applications"))
											  .Elements()
											  .Select(e =>
											  {
												  var visual = e.Element(m1 + "VisualElements") ??
															   e.Element(m2 + "VisualElements") ??
															   e.Element(m3 + "VisualElements") ??
															   e.Element(m4 + "VisualElements");
												  if (visual == null) return null;
												  return new
												  {
													  Id = e.Attribute("Id").Value,
													  File = (e.Attribute("Executable") ?? e.Attribute("StartPage")).Value,
													  Description = visual.GetValueOrNull("Description"),
													  DisplayName = visual.GetValueOrNull("DisplayName"),
													  Logos = visual.Attributes()
																	.Where(a => a.Name.LocalName.Contains("Logo"))
																	.Select(a => new
																	{
																		Name = a.Name.LocalName,
																		RelativePath = a.Value
																	}),
													  //BackgroundColor="#000000" ForegroundText="light" 
													  ForegroundText = (visual.GetValueOrNull("ForegroundText")?? string.Empty)
																			 .ToLower()
																			 .Equals("light") ? Colors.White : Colors.Black,
													  BackgroundColor = ParseColor(visual.GetValueOrNull("BackgroundColor"))
												  };
											  });

                foreach (var appDataItem in appDataItems)
                {
                    if(appDataItem == null) continue;
                    Log.Raw("Processing manifest item with ID " + appDataItem.Id);
                    var suffix = appDataItem.Id;
                    var appUserModelId = package.Value
                                                .Where(s => !s.ServerId.StartsWith("Background"))
                                                .Where(s => !s.ExePath.Contains("\\background"))
                                                .Where(s => s.AppUserModelId.EndsWith("!" + suffix))
                                                .Select(s => s.AppUserModelId)
                                                .Distinct()
                                                .Single();
                    Log.Raw("AppUserModelId identified is " + appUserModelId);
                    yield return new ImmersiveApp
                    {
                        PackageId = package.Key,
                        File = appDataItem.File,
                        ApplicationName = Util.ExtractStringIndirect(pathCacheItem.DisplayName),
                        ApplicationDisplayName = ExtractStringFromPackageOptional(appDataItem.DisplayName, pathCacheItem.DisplayName),
                        ApplicationCompany = ExtractStringFromPackageOptional(company, pathCacheItem.DisplayName),
                        ApplicationDescription = ExtractStringFromPackageOptional(appDataItem.Description, pathCacheItem.DisplayName),
                        AppUserModelID = appUserModelId,
                        Background = appDataItem.BackgroundColor,
                        Foreground = appDataItem.ForegroundText,
                        ApplicationPath = pathCacheItem.RootPath,
                        Logos = appDataItem.Logos
                                           .ToDictionary(l => l.Name,
                                                         l => LocateLogo(logoTemplate,
                                                                         l.RelativePath,
                                                                         pathCacheItem.DisplayName,
                                                                         pathCacheItem.RootPath,
                                                                         package.Key))
                    };
                }
            }
        }

        private static Dictionary<string, List<ServerCacheItem>> GetExecutableServerCache()
        {
            var serverCache = new Dictionary<string, List<ServerCacheItem>>();
            using (var mainRepo = Registry.ClassesRoot.OpenSubKey("ActivatableClasses\\Package"))
            {
                if (mainRepo == null)
                {
                    Log.Raw("Repo couldn't be opened.");
                    return null;
                }
                Log.Raw("Repo openeed.");
                foreach (var packageKey in mainRepo.GetSubKeyNames())
                {
                    Log.Raw("Processing", packageKey);
                    using (var subKey = mainRepo.OpenSubKey(packageKey + "\\Server"))
                    {
                        if (subKey == null) continue;
                        Log.Raw("Processing servers", packageKey);

                        var serverList = new List<ServerCacheItem>();
                        serverCache[packageKey] = serverList;

                        foreach (var serverKeyName in subKey.GetSubKeyNames())
                        {
                            Log.Raw("Processing server ", serverKeyName);
                            using (var serverKey = subKey.OpenSubKey(serverKeyName))
                            {
                                serverList.Add(new ServerCacheItem
                                {
                                    ServerId = serverKeyName,
// ReSharper disable once PossibleNullReferenceException
                                    AppUserModelId = (string) serverKey.GetValue("AppUserModelId"),
                                    ExePath = serverKey.GetValue("ExePath").ToString().ToLower()
                                });
                            }
                        }
                    }
                }
            }
            return serverCache;
        }

        private static Dictionary<string, PathCacheItem> GetDeploymentPathCache()
        {
            const string repoPath =
                "Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\Repository\\Packages";
            var pathCache = new Dictionary<string, PathCacheItem>();
            using (var packageRepo = Registry.ClassesRoot.OpenSubKey(repoPath))
            {
                if (packageRepo == null)
                {
                    Log.Raw("Repo couldn't be opened.");
                    return null;
                }
                Log.Raw("Repo openeed.");
                foreach (var keyName in packageRepo.GetSubKeyNames())
                {
                    Log.Raw("Processing", keyName);
                    using (var subKey = packageRepo.OpenSubKey(keyName))
                    {
// ReSharper disable once PossibleNullReferenceException
                        pathCache[(string) subKey.GetValue("PackageID")] = new PathCacheItem
                        {
                            DisplayName = (string) subKey.GetValue("DisplayName"),
                            RootPath = (string) subKey.GetValue("PackageRootFolder")
                        };
                    }
                }
            }
            return pathCache;
        }

        public static bool Any
        {
            get { return GetAppsCache().Count > 0; }
        }

        //Internal helpers

        private static string LocateLogo(string logoTemplate, string logoResourcePath, string uriTemplate, string rootPath, string packageKey)
        {
            var straightforward = Path.Combine(rootPath, logoResourcePath);
            if (File.Exists(straightforward)) return straightforward;

            if (!uriTemplate.StartsWith("@{"))
            {
                //"@{FileManager_6.3.9600.16384_neutral_neutral_cw5n1h2txyewy?ms-resource://FileManager/Files/Assets/SkyDriveLogo.png}"
                int i = 0, j = packageKey.Length - 1;
                while (i < 4)
                {
                    if (packageKey[j] == '_') ++i;
                    --j;
                }
                var rootItem = packageKey.Substring(0, j + 1);
                uriTemplate = string.Format("@{{{0}?ms-resource://{1}/resources/data}}", packageKey, rootItem);
            }
            const string prefix = "ms-resource:Files/";
            string fallback = null;
            if (logoTemplate != null)
            {
                fallback = ExtractStringFromPackageOptional(prefix + logoTemplate.Replace('\\', '/'), uriTemplate);
            }

            var lastslash = logoResourcePath.LastIndexOf('\\');
            if (lastslash == -1 && logoTemplate != null)//just filename - overwritelogo
            {
                var templateLastSlash = logoTemplate.LastIndexOf('\\');
                var actual = ExtractStringFromPackageOptional(prefix + logoTemplate.Substring(0, templateLastSlash).Replace('\\', '/') + '/' + logoResourcePath, uriTemplate);
                return string.IsNullOrEmpty(actual) ? fallback : actual;

            }
            else //relative path
            {
                var actual = ExtractStringFromPackageOptional(prefix + logoResourcePath.Replace('\\', '/'), uriTemplate);
                return string.IsNullOrEmpty(actual) ? fallback : actual;
            }
        }

        private static Color ParseColor(string value)
        {
            if (value[0] == '#')
            {
                return Color.FromRgb(HexCharToByte(value[1], value[2]),
                                     HexCharToByte(value[3], value[4]),
                                     HexCharToByte(value[5], value[6]));

            }
            const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public;
            return (Color)typeof(Colors).GetProperty(value, flags).GetValue(null, null);
        }

        private static byte HexCharToByte(char ch1, char ch2)
        {
            return (byte) (HexCharToByte(ch1)*16 + HexCharToByte(ch2));
        }

        private static int HexCharToByte(char ch1)
        {
            ch1 -= '0';
            return ch1 > 9 ? (ch1 > 41 ? ch1 - 39 : ch1 - 7) : ch1;
        }
        
        private static string ExtractStringFromPackageOptional(string resourceKey, string uriTemplate)
        {
            if (resourceKey == null) return null;
            if (resourceKey.StartsWith("ms-resource:"))
            {
                var pair = uriTemplate.Split(new[] { '?' }, 2);

                var uri = new UriBuilder(pair[1].TrimEnd('}'));
                var uriParts = uri.Path.Split('/').ToList();
                var localParts = new Uri(resourceKey).LocalPath.Split('/');
                for (int i = localParts.Length - 1; i >= 0; i--)
                {
                    var targetIdx = uriParts.Count - (localParts.Length - i);
                    if (targetIdx >= 0) uriParts.RemoveAt(targetIdx);
                    uriParts.Insert(Math.Max(targetIdx, 0), localParts[i]);
                }
                uri.Path = string.Join("/", uriParts);

                return Util.ExtractStringIndirect(pair[0] + "?" + uri + "}");

            }
            return resourceKey;
        }

        private static string GetValueOrNull(this XElement xe, string nullForValueOrAttributeName = null)
        {
            if (xe == null) return null;
            if (nullForValueOrAttributeName == null) return xe.Value;
            var attribute = xe.Attribute(nullForValueOrAttributeName);
            return attribute == null ? null : attribute.Value;
        }
        
        //Internal classes 

        class PathCacheItem
        {
            public string DisplayName, RootPath;
        }

        class ServerCacheItem
        {
            public string AppUserModelId, ServerId, ExePath;
        }
    }
}

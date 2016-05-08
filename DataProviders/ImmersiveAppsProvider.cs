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
                        _cache = new List<ImmersiveApp>(GetApps().OrderBy(x => x.DisplayName));
            return _cache;
        }

        private static IEnumerable<ImmersiveApp> GetApps()
        {
            Log.Raw("Begin GetApps. OsIs 8+ = " + Util.OsIs.EightOrMore);
            //Only for Win8+
            if (!Util.OsIs.EightOrMore) yield break;

            //Step 1. Getting initial package deployment info: package ids and related deployment paths and display names
            //This may include non-activateable or non-existing packages
            var pathCache = GetAppPackages();
            if (pathCache == null || pathCache.Count == 0) yield break;

            //Step 2. Scanning activateable classes repo to filter activatable package IDs
            var serverCache = GetExecutablePackages();
            if (serverCache == null || serverCache.Length == 0) yield break;

            //Step 3. Corellating package IDs to App families. We need this info because AppUserModelId is
            //(only for immersive apps) "<FamilyId>!<AppId>". As a side note, we will get App IDs further.
            var families = GetAppFamilies();
            if (families == null || families.Count == 0) yield break;

            //Step 4. Parse app manifest to get more visual information and corellate deployment and server caches,
            //and (the most important!) get the AppIds from which we'll construct the AppUserModelIds
            XNamespace m1 = XNamespace.Get("http://schemas.microsoft.com/appx/2010/manifest"), //W8 old
                       m2 = XNamespace.Get("http://schemas.microsoft.com/appx/2013/manifest"), //W8 new
                       m3 = XNamespace.Get("http://schemas.microsoft.com/appx/manifest/foundation/windows10"),
                       m4 = XNamespace.Get("http://schemas.microsoft.com/appx/manifest/uap/windows10"),
                       m0;

            foreach (var package in serverCache)// for all the activateable classes
            {
                PathCacheItem pathCacheItem;
                if (!pathCache.TryGetValue(package, out pathCacheItem))
                {
                    Log.Raw("Skipping activateable package (reason: path cache miss)", package);
                    continue;
                }

                string family;
                if (!families.TryGetValue(package, out family))
                {
                    Log.Raw("Skipping activateable package (reason: family cache miss)", package);
                    continue;
                }

                Log.Raw("Loading package data", package);
                var appxManifestPath = Path.Combine(pathCacheItem.RootPath, APPXMANIFEST_XML);
                if (!File.Exists(appxManifestPath))
                {
                    Log.Raw("Appx Manifest not found", appxManifestPath);
                    continue;
                }

                string company = null, logoTemplate = null;

                Log.Raw("Processing manifest " + appxManifestPath);
                var appManifest = XElement.Load(appxManifestPath);
                m0 = appManifest.Name.Namespace;
                var props = appManifest.Element(m0 + "Properties");
                if (props != null)
                {
                    company = props.Element(m0 + "PublisherDisplayName").GetValueOrNull();
                    logoTemplate = props.Element(m0 + "Logo").GetValueOrNull();
                    //there's also display name but we already have it - in pathCacheItem
                }
                var apps = appManifest.Element(m0 + "Applications");
                if (apps == null)
                {
                    Log.Raw("Skipping activateable package (reason: no apps in manufest file)", package);
                    continue;
                }
                var appDataItems = apps.Elements()
                                       .Select(e =>
                                       {
                                           var visual = e.Element(m1 + "VisualElements") ??
                                                        e.Element(m2 + "VisualElements") ??
                                                        e.Element(m3 + "VisualElements") ??
                                                        e.Element(m4 + "VisualElements");
                                           if (visual == null) return null;
                                           var executable = e.Attribute("Executable") ?? e.Attribute("StartPage");
                                           if (executable == null) return null;
                                           return new
                                           {
                                               Id = e.Attribute("Id").Value,
                                               File = executable.Value,
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
                                               ForegroundText = ParseColorBoolean(visual.GetValueOrNull("ForegroundText")),
                                               BackgroundColor = ParseColor(visual.GetValueOrNull("BackgroundColor"))
                                           };
                                       });
                var uriTemplates = appDataItems.Where(a=>a!=null)
                                               .Select(a => a.DisplayName)
                                               .Where(d => d != null && d.ToLower().Contains("ms-resource://"))
                                               .Union(new[] { pathCacheItem.DisplayName, "default" })
                                               .ToArray();

                for (int k = 0; k < uriTemplates.Length; k++)
                {
                    if (!uriTemplates[k].StartsWith("@{"))
                    {
                        //"@{FileManager_6.3.9600.16384_neutral_neutral_cw5n1h2txyewy?ms-resource://FileManager/Files/Assets/SkyDriveLogo.png}"
                        if (uriTemplates[k].StartsWith("ms-resource://")) //just wrap in resolvable format
                        {
                            uriTemplates[k] = string.Format("@{{{0}?{1}}}", package, uriTemplates[k]);
                        }
                        else //construct anew
                        {
                            int i = 0, j = package.Length - 1;
                            while (i < 4)
                            {
                                if (package[j] == '_') ++i;
                                --j;
                            }
                            var rootItem = package.Substring(0, j + 1);
                            uriTemplates[k] = string.Format("@{{{0}?ms-resource://{1}/resources/data}}", package, rootItem);
                        }
                    }
                }

                foreach (var appDataItem in appDataItems)
                {
                    if (appDataItem == null) continue;
                    if (appDataItem.Id.Equals("Designer.App", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Raw("Skipping VisualStudio designer-generated app");
                        continue;
                    }
                    Log.Raw("Processing application with ID " + appDataItem.Id);
                    var appUserModelId = string.Format("{0}!{1}", family, appDataItem.Id);
                    Log.Raw("AppUserModelId constructed is " + appUserModelId);
                    yield return new ImmersiveApp
                    {
                        PackageId = package,
                        File = appDataItem.File,
                        ApplicationName = Util.ExtractStringIndirect(pathCacheItem.DisplayName),
                        ApplicationDisplayName = ExtractStringFromPackageOptional(appDataItem.DisplayName, uriTemplates),
                        ApplicationCompany = ExtractStringFromPackageOptional(company, uriTemplates),
                        ApplicationDescription = ExtractStringFromPackageOptional(appDataItem.Description, uriTemplates),
                        AppUserModelID = appUserModelId,
                        Background = appDataItem.BackgroundColor,
                        Foreground = appDataItem.ForegroundText,
                        ApplicationPath = pathCacheItem.RootPath,
                        Logos = appDataItem.Logos
                                           .ToDictionary(l => l.Name,
                                                         l => LocateLogo(logoTemplate,
                                                                         l.RelativePath,
                                                                         uriTemplates,
                                                                         pathCacheItem.RootPath))
                    };
                }
            }
        }

        public static bool Any
        {
            get { return GetAppsCache().Count > 0; }
        }

        public static Dictionary<string, string> GetAppFamilies()
        {
            const string repoPath =
                "Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\Repository\\Families";
            var families = new Dictionary<string, string>();
            using (var familiesListKey = Registry.ClassesRoot.OpenSubKey(repoPath))
            {
                if (familiesListKey == null)
                {
                    Log.Raw("Repo couldn't be opened.");
                    return null;
                }
                Log.Raw("Repo openeed.");
                foreach (var family in familiesListKey.GetSubKeyNames())
                {
                    Log.Raw("Processing", family);
                    using (var familyKey = familiesListKey.OpenSubKey(family))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        foreach (var app in familyKey.GetSubKeyNames())
                        {
                            families[app] = family;
                        }
                    }
                }
            }
            return families;
        }

        private static Dictionary<string, PathCacheItem> GetAppPackages()
        {
            const string repoPath =
                "Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\Repository\\Packages";
            var pathCache = new Dictionary<string, PathCacheItem>();
            using (var packageRepoKey = Registry.ClassesRoot.OpenSubKey(repoPath))
            {
                if (packageRepoKey == null)
                {
                    Log.Raw("Repo couldn't be opened.");
                    return null;
                }
                Log.Raw("Repo openeed.");
                foreach (var package in packageRepoKey.GetSubKeyNames())
                {
                    Log.Raw("Processing", package);
                    using (var packageKey = packageRepoKey.OpenSubKey(package))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        pathCache[package] = new PathCacheItem
                        {
                            DisplayName = (string)packageKey.GetValue("DisplayName"),
                            RootPath = (string)packageKey.GetValue("PackageRootFolder")
                        };

                    }
                }
            }
            return pathCache;
        }

        private static string[] GetExecutablePackages()
        {
            using (var mainRepo = Registry.ClassesRoot.OpenSubKey("ActivatableClasses\\Package"))
            {
                if (mainRepo == null)
                {
                    Log.Raw("Repo couldn't be opened.");
                    return null;
                }
                Log.Raw("Repo openeed.");
                return mainRepo.GetSubKeyNames();
            }
        }

        //Internal helpers

        private static string LocateLogo(string logoTemplate, string logoResourcePath, string[] uriTemplates, string rootPath)
        {
            var straightforward = Path.Combine(rootPath, logoResourcePath);
            if (File.Exists(straightforward)) return straightforward;

            const string prefix = "ms-resource:Files/";
            string fallback = null;
            if (logoTemplate != null)
            {
                fallback = ExtractStringFromPackageOptional(prefix + logoTemplate.Replace('\\', '/'), uriTemplates);
            }

            var lastslash = logoResourcePath.LastIndexOf('\\');
            if (lastslash == -1 && logoTemplate != null)//just filename - overwritelogo
            {
                var templateLastSlash = logoTemplate.LastIndexOf('\\');
                var intermediate = templateLastSlash == -1 ? string.Empty : (logoTemplate.Substring(0, templateLastSlash).Replace('\\', '/') + '/');
                var actual = ExtractStringFromPackageOptional(prefix + intermediate + logoResourcePath, uriTemplates);
                return string.IsNullOrEmpty(actual) ? fallback : actual;

            }
            else //relative path
            {
                var actual = ExtractStringFromPackageOptional(prefix + logoResourcePath.Replace('\\', '/'), uriTemplates);
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
            if ("transparent".Equals(value, StringComparison.OrdinalIgnoreCase) && Util.OsIs.TenOrMore)
            {
                return Colors.Black;
            }
            const BindingFlags flags = BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public;
            return (Color)typeof(Colors).GetProperty(value, flags).GetValue(null, null);
        }

        private static Color ParseColorBoolean(string value)
        {
            if (value != null)
            {
                if (value.Equals("light", StringComparison.OrdinalIgnoreCase))
                    return Colors.White;
                if (value.Equals("dark", StringComparison.OrdinalIgnoreCase))
                    return Colors.Black;
            }
            return Util.OsIs.TenOrMore ? Colors.White : Colors.Red;
        }

        private static byte HexCharToByte(char ch1, char ch2)
        {
            return (byte)(HexCharToByte(ch1) * 16 + HexCharToByte(ch2));
        }

        private static int HexCharToByte(char ch1)
        {
            ch1 -= '0';
            return ch1 > 9 ? (ch1 > 41 ? ch1 - 39 : ch1 - 7) : ch1;
        }

        private static string ExtractStringFromPackageOptional(string resourceKey, string[] uriTemplates)
        {
            if (resourceKey == null) return null;
            if (resourceKey.StartsWith("ms-resource:"))
            {
                foreach (var uriTemplate in uriTemplates)
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

                    var result = Util.ExtractStringIndirect(pair[0] + "?" + uri + "}");
                    if (result != null) return result;
                }
                return null;
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
    }
}

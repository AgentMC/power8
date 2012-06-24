using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Power8
{
    public static class MfuList
    {
        private static ObservableCollection<PowerItem> _startMfu;
        public static ObservableCollection<PowerItem> StartMfu
        {
            get
            {
                if(_startMfu == null)
                {
                    _startMfu = new ObservableCollection<PowerItem>();
                    UpdateStartMfu();
                }
                return _startMfu;
            }
        }

        private static readonly string[] MsFilter;
        static MfuList()
        {
            using(var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                   @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileAssociation", 
                   false))
            {
                var col = new List<String>();
// ReSharper disable AccessToDisposedClosure
                if (k != null)
                {
                    foreach (var val in new[] {"AddRemoveApps", "AddRemoveNames", "HostApps"}
                        .Select(kName => (string) k.GetValue(kName, null))
                        .Where(val => !string.IsNullOrEmpty(val)))
                    {
                        col.AddRange(val.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
// ReSharper restore AccessToDisposedClosure
                col.Add("APPLICATION SHORTCUTS");
                col.Add("HOST.EXE");
                MsFilter = col.ToArray();
            }
        }

        private const string USERASSISTKEY = @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist\{0}\Count";
        //TODO: rewrite os-specific selection
        private static class Guids
        {
            public const string W7_1 = "{CEBFF5CD-ACE2-4F4F-9178-9926F41749EA}";
            public const string W7_2 = "{F4E57C4B-2036-45F0-A9AB-443BCFE33D9F}";
            public const string XP_1 = "{75048700-EF1F-11D0-9888-006097DEACF9}";
            public const string XP_2 = "{5E6AB780-7743-11CF-A12B-00AA004AE837}";
        }

        private static List<MfuElement> _lastList;
        public static void UpdateStartMfu()
        {
#if DEBUG
            var sss = System.Diagnostics.Stopwatch.StartNew();
# endif
            //Step 1: parse registry
            var list = new List<MfuElement>();
            string ks1, ks2;
            if (Environment.OSVersion.Version.Major == 5)
            {
                ks1 = string.Format(USERASSISTKEY, Guids.XP_1);
                ks2 = string.Format(USERASSISTKEY, Guids.XP_2);
            }
            else
            {
                ks1 = string.Format(USERASSISTKEY, Guids.W7_1);
                ks2 = string.Format(USERASSISTKEY, Guids.W7_2);
            }
            var k1 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(ks1, false);
            var k2 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(ks2, false);
            if(k1 != null && k2 != null)
            {
                foreach (var k in new[]{k1, k2})
                {
                    if (Environment.OSVersion.Version.Major == 5)
                    {
                        list.AddRange((from valueName in k.GetValueNames()
                                       let data = (byte[])k.GetValue(valueName)
                                       let fileTime = data.Length == 16 ? BitConverter.ToInt64(data, 8) : 0
                                       where fileTime != 0 && valueName.Contains("\\")
                                       select new MfuElement
                                       {
                                           Arg = DeRot13AndKnwnFldr(valueName),
                                           LaunchCount = BitConverter.ToInt32(data, 4) - 5,
                                           LastLaunchTimeStamp =
                                               DateTime.FromFileTime(fileTime)
                                       }).Where(mfu => mfu.IsOk()));
                    }
                    else
                    {
                        list.AddRange((from valueName in k.GetValueNames()
                                       let data = (byte[]) k.GetValue(valueName)
                                       let fileTime = data.Length == 72 ?  BitConverter.ToInt64(data, 60) : 0
                                       where fileTime != 0 && valueName.Contains("\\")
                                       select new MfuElement
                                                  {
                                                      Arg = DeRot13AndKnwnFldr(valueName),
                                                      LaunchCount = BitConverter.ToInt32(data, 4),
                                                      LastLaunchTimeStamp =
                                                          DateTime.FromFileTime(fileTime)
                                                  }).Where(mfu => mfu.IsOk()));
                    }
                }
                k1.Close();
                k2.Close();
            }

            //Step 2: filter and sort
            var linx = list
                .FindAll(m => m.Arg.EndsWith(".lnk", StringComparison.InvariantCultureIgnoreCase))
                .Select(l => Util.ResolveLink(l.Arg))
                .ToArray();
            list.RemoveAll(l => linx.Contains(l.Arg, StringComparer.InvariantCultureIgnoreCase));
            list.ApplyMsFilter();
            if(_lastList == null || list.Except(_lastList).Any())
            {
                list.Sort();

                //Step 3: update collection
                lock (_startMfu)
                {
                    _lastList = list;
                    _startMfu.Clear();
                    foreach (var mfuElement in list)
                    {
                        _startMfu.Add(new PowerItem
                                          {
                                              Argument = mfuElement.Arg,
                                          });
                    }
                }    
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Update Start MFU: done in " + sss.ElapsedMilliseconds);
            sss.Stop();
#endif
        }

        private static string Rot13(string s)
        {
            var r13 = new StringBuilder(s.Length);
            var bytes = Encoding.ASCII.GetBytes(s);
            var a = Encoding.ASCII.GetBytes("A")[0];
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    var lcaseMarker = (byte)(bytes[i] & 32);
                    bytes[i] = (byte) (((bytes[i] & (~32 & 0xFF)) - a + 13)%26 + a);
                    bytes[i] |= lcaseMarker;
                    r13.Append(Encoding.ASCII.GetChars(bytes, i, 1));
                }
                else
                {
                    r13.Append(s[i]);
                }
            }
            return r13.ToString();
        }

        private static string DeRot13AndKnwnFldr(string s)
        {
            s = Rot13(s);
            if(s.StartsWith("{"))
            {
                var pair = s.Split(new[] {'}'}, 2);
                return Util.ResolveKnownFolder(pair[0].Substring(1)) + pair[1];
            }
            if (s.StartsWith("UEME_"))
            {
                s = s.Split(new[] {':'}, 2)[1];
            }
            if (s.StartsWith("%csidl"))
            {
                var pair = s.Split(new[] {'%'}, 2, StringSplitOptions.RemoveEmptyEntries);
                s = Util.ResolveSpecialFolder((API.Csidl) int.Parse(pair[0].Substring(5))) + pair[1];
            }
            return s;
        }

        private static void ApplyMsFilter(this List<MfuElement> list )
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var ucase = list[i].Arg.ToUpperInvariant();
                if(MsFilter.Any(ucase.Contains))
                    list.RemoveAt(i);
            }
        }

        private struct MfuElement : IComparable<MfuElement>
        {
            public string Arg;
            public int LaunchCount;
            public DateTime LastLaunchTimeStamp;

            public int CompareTo(MfuElement other)
            {
                return LaunchCount == other.LaunchCount 
                    ? -LastLaunchTimeStamp.CompareTo(other.LastLaunchTimeStamp) 
                    : -LaunchCount.CompareTo(other.LaunchCount);
            }

            public bool IsOk()
            {
                return LaunchCount > -1
                       && LastLaunchTimeStamp != DateTime.MinValue
                       && System.IO.File.Exists(Arg);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Power8.Properties;

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
                    MfuSearchRoot = new PowerItem(_startMfu) {FriendlyName = Resources.Str_Recent};
                    UpdateStartMfu();
                }
                return _startMfu;
            }
        }
        public static PowerItem MfuSearchRoot;


        private static readonly List<MfuElement> LastList = new List<MfuElement>();
        private static readonly List<MfuElement> P8JlImpl = new List<MfuElement>();
        private static readonly string[] MsFilter;
        private static readonly ManagementEventWatcher WatchDog;
        private static readonly int SessionId = Process.GetCurrentProcess().SessionId;

        private static readonly string DataBase = 
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Power8_Team\\LaunchData.csv";

        private const string
            USERASSISTKEY = @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist\{0}\Count",
            PLD_SPLITTER = "<:;:>";

        static class Guids
        {
            public const string W7_1 = "{CEBFF5CD-ACE2-4F4F-9178-9926F41749EA}";
            public const string W7_2 = "{F4E57C4B-2036-45F0-A9AB-443BCFE33D9F}";
            public const string XP_1 = "{75048700-EF1F-11D0-9888-006097DEACF9}";
            public const string XP_2 = "{5E6AB780-7743-11CF-A12B-00AA004AE837}";
        }


        
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

                if(File.Exists(DataBase))
                {
                    using (var f = new StreamReader(DataBase, Encoding.UTF8))
                    {
                        while (!f.EndOfStream)
                        {
                            var l = f.ReadLine();
                            if (string.IsNullOrEmpty(l)) 
                                continue;
                            var ls = l.Split(new[]{PLD_SPLITTER}, 4, StringSplitOptions.None);
                            if(ls.Length != 4)
                                continue;
                            P8JlImpl.Add(new MfuElement
                                             {
                                                 Arg = ls[0],
                                                 Cmd = ls[1],
                                                 LaunchCount = int.Parse(ls[2]),
                                                 LastLaunchTimeStamp =
                                                     DateTime.ParseExact(ls[3], "yyyyMMddHHmmss", CultureInfo.CurrentCulture)
                                             });
                        }
                    }
                }

                Util.MainDisp.ShutdownStarted += MainDispOnShutdownStarted;

                WatchDog = new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'");
                WatchDog.EventArrived += WatchDogOnEventArrived;
                WatchDog.Start();
            }
        }

        private static void MainDispOnShutdownStarted(object sender, EventArgs eventArgs)
        {
// ReSharper disable AssignNullToNotNullAttribute
            Directory.CreateDirectory(Path.GetDirectoryName(DataBase));
// ReSharper restore AssignNullToNotNullAttribute
            using (var f = new StreamWriter(DataBase, false, Encoding.UTF8))
            {
                foreach (var mfuElement in P8JlImpl)
                {
                    f.WriteLine("{0}{4}{1}{4}{2}{4}{3}",
                                mfuElement.Arg,
                                mfuElement.Cmd,
                                mfuElement.LaunchCount,
                                mfuElement.LastLaunchTimeStamp.ToString("yyyyMMddHHmmss"),
                                PLD_SPLITTER);
                }
            }
        }

        private static void WatchDogOnEventArrived(object sender, EventArrivedEventArgs e)
        {
            var proc = e.NewEvent["TargetInstance"] as ManagementBaseObject;
            if (proc == null) 
                return;
            var sId = (int)(uint) proc["SessionId"];
            if (sId != SessionId) 
                return;
            var cmd = (string) proc["CommandLine"];
            if(!string.IsNullOrEmpty(cmd) && !MsFilter.Any(cmd.ToUpper().Contains))
            {
                var pair = Util.CommandToFilenameAndArgs(cmd);
                pair = Tuple.Create(pair.Item1.ToLowerInvariant(), pair.Item2.ToLowerInvariant());
                if(string.IsNullOrEmpty(pair.Item2))
                    return; //System will record usage, we don't need to monitor commandless launch
                if (string.IsNullOrEmpty(pair.Item1) || pair.Item1.Length < 2 || pair.Item1[1] != ':' || !File.Exists(pair.Item1))
                    return; //As a rule, user-launched applications have full path. Something as "rundll %1 %2 %3" won't make sence for P8
                string checkPath = pair.Item2;
                bool exists = File.Exists(checkPath);
                if (!exists && checkPath.StartsWith("\"") && checkPath.EndsWith("\""))
                {
                    checkPath = checkPath.Substring(1, checkPath.Length - 2);
                    exists = File.Exists(checkPath);
                    if(!exists)
                        checkPath = pair.Item2;
                }
                var prefix = exists ? string.Empty : "::";
                pair = Tuple.Create(pair.Item1.ToLowerInvariant(), prefix + checkPath.ToLowerInvariant());
                var t = P8JlImpl.Find(j => j.Arg == pair.Item1 && j.Cmd == pair.Item2);
                if(t == null)
                {
                    P8JlImpl.Add(new MfuElement
                                     {
                                         Arg = pair.Item1,
                                         Cmd = pair.Item2,
                                         LaunchCount = 1,
                                         LastLaunchTimeStamp = DateTime.Now
                                     });
                }
                else
                {
                    t.LaunchCount += 1;
                    t.LastLaunchTimeStamp = DateTime.Now;
                }
#if DEBUG
                Debug.WriteLine("Process Launched: {0} {1}", pair.Item1, pair.Item2);
#endif
            }
        }



        public static void UpdateStartMfu()
        {
            Util.Fork(UpdateStartMfuSync, "Update MFU").Start();
        }

        public static void UpdateStartMfuSync()
        {
        //Step 1: parse registry
            var list = new List<MfuElement>();
            string ks1, ks2;
            int dataWidthExpected, fileTimeOffset, launchCountCorrection;
            if (Util.OsIs.XPOrLess)
            {
                ks1 = string.Format(USERASSISTKEY, Guids.XP_1);
                ks2 = string.Format(USERASSISTKEY, Guids.XP_2);
                dataWidthExpected = 16;
                fileTimeOffset = 8;
                launchCountCorrection = 5;
            }
            else
            {
                ks1 = string.Format(USERASSISTKEY, Guids.W7_1);
                ks2 = string.Format(USERASSISTKEY, Guids.W7_2);
                dataWidthExpected = 72;
                fileTimeOffset = 60;
                launchCountCorrection = 0;
            }
            var k1 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(ks1, false);
            var k2 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(ks2, false);
            if(k1 != null && k2 != null)
            {
                foreach (var k in new[]{k1, k2})
                {
                    list.AddRange(
                        (from valueName in k.GetValueNames()
                        let data = (byte[])k.GetValue(valueName)
                        let fileTime = data.Length == dataWidthExpected ? BitConverter.ToInt64(data, fileTimeOffset) : 0
                        where fileTime != 0 && valueName.Contains("\\")
                        select new MfuElement
                        {
                            Arg = DeRot13AndKnwnFldr(valueName),
                            LaunchCount = BitConverter.ToInt32(data, 4) - launchCountCorrection,
                            LastLaunchTimeStamp = DateTime.FromFileTime(fileTime)
                        })
                        .Where(mfu => mfu.IsOk())
                    );
                }
                k1.Close();
                k2.Close();
            }

            if (!list.Except(LastList).Any()) 
                return;

            LastList.Clear();
            list.ForEach(m => LastList.Add(m.Clone()));

            lock (_startMfu)
            {
                //Step 2.1: filter out setup, host apps and documentation files
                list.ApplyMsFilter();
                
                //Step 2.2: filter out malformed paths
                var malformed = list.FindAll(m => m.Arg.Contains("\\\\") && !m.Arg.StartsWith("\\"));
                malformed.ForEach(mf =>
                                      {
                                          var properArg = mf.Arg.Replace("\\\\", "\\");
                                          mf.Arg = properArg;
                                          var existent = list.Find(m => m.Arg == properArg);
                                          list.Remove(existent);
                                          mf.Mix(existent);
                                      });
                
                //Step 2.3: filter out direct paths to the objects for which we have links
                var linx = (from l in
                                (from m in list
                                 where m.Arg.EndsWith(".lnk", StringComparison.InvariantCultureIgnoreCase)
                                 select Tuple.Create(Util.ResolveLink(m.Arg), m))
                            orderby l.Item1
                            select l).ToList();
                //Step 2.3.1 : removing dupliocate links
                for (int i = linx.Count - 1; i >= 0; i--)
                {
                    if(i > 0 && linx[i-1].Item1.Equals(linx[i].Item1, StringComparison.InvariantCultureIgnoreCase))
                    {
                        linx[i - 1].Item2.Mix((linx[i].Item2));
                        list.Remove(linx[i].Item2);
                        linx.RemoveAt(i);
                    }
                }
                //Step 2.3.2: really filter out
                foreach (var tuple in linx)
                {
                    var directs =
                        list.FindAll(q => q.Arg.Equals(tuple.Item1, StringComparison.InvariantCultureIgnoreCase));
                    foreach (var direct in directs)
                    {
                        list.Remove(direct);
                        tuple.Item2.Mix(direct);
                    }
                }

                //Step 2.4: sort.
                list.Sort();
                
                //Step 2.5: limit single/zero-used to 20 items
                for (int i = 0, j = 0; i < list.Count; i++)
                {
                    if (list[i].LaunchCount<2 && ++j > 20)
                    {
                        list.RemoveRange(i, list.Count - i);
                        break;
                    }
                }
                
                //Step 3: update collection
                Util.Post(() =>
                              {
                                  _startMfu.Clear();
                                  foreach (var mfuElement in list)
                                      _startMfu.Add(new PowerItem {Argument = mfuElement.Arg, Parent = MfuSearchRoot});
                              }
                            );
            }
        }

        public static void GetRecentListFor(PowerItem item)
        {
            Util.Fork(() => GetRecentListForSync(item), "MFU worker for " + item.Argument).Start();
        }

        private static void GetRecentListForSync(PowerItem item)
        {
            var fsObject = PowerItemTree.GetResolvedArgument(item);
            IEnumerable<string> jl = null;
            var p8R = GetP8Recent(item.IsLink ? item.ResolvedLink : fsObject);
            if(Util.OsIs.SevenOrMore)
            {
                var recent = GetJumpList(fsObject, API.ADLT.RECENT);
                var frequent = GetJumpList(fsObject, API.ADLT.FREQUENT);
                if (recent != null && frequent != null)
                    jl = recent.Union(frequent);
                else
                    jl = recent ?? frequent;
                if (jl != null)
                    jl = jl.Where(x => x.StartsWith("::") || File.Exists(x));
            }
            if (jl != null && p8R != null)
                jl = jl.Union(p8R);
            else
                jl = jl ?? p8R;
            if (jl != null)
            {
                foreach (var arg in jl)
                {
                    var local = arg;
                    Util.Post(() =>
                              item.JumpList.Add(local.StartsWith("::")
                                                    ? new PowerItem
                                                          {
                                                              Argument = local.Substring(2),
                                                              Parent = item,
                                                              SpecialFolderId = API.Csidl.POWER8JLITEM
                                                          }
                                                    : new PowerItem
                                                          {
                                                              Argument = local,
                                                              Parent = item
                                                          }
                                  ));
                }
            }

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

        private static IEnumerable<string> GetJumpList(string fsObject, API.ADLT listType)
        {
            var riidPropertyStore = new Guid(API.IID_IPropertyStore);
            API.IPropertyStore store;
            var res = API.SHGetPropertyStoreFromParsingName(fsObject,
                                                            IntPtr.Zero,
                                                            API.GETPROPERTYSTOREFLAGS.GPS_DEFAULT,
                                                            ref riidPropertyStore,
                                                            out store);
            if (res > 0)
                return null;
            using (var pv2 = new API.PROPVARIANT())
            {
                store.GetValue(API.PKEY.AppUserModel_ID, pv2);
                Marshal.FinalReleaseComObject(store);
                if (pv2.longVal == 0)
                    return null;

                var ret = new Collection<string>();
                var listProvider = (API.IApplicationDocumentLists)new API.ApplicationDocumentLists();
                listProvider.SetAppID(pv2.GetValue());
                var riidObjectArray = new Guid(API.IID_IObjectArray);
                var list = (API.IObjectArray)listProvider.GetList(listType, 0, ref riidObjectArray);

                if (list != null)
                {
                    var riidShellItem = new Guid(API.IID_IShellItem);
                    var riidShellLink = new Guid(API.IID_IShellLinkW);
                    for (uint i = 0; i < list.GetCount(); i++)
                    {
                        object item;
                        try
                        {
                            item = list.GetAt(i, ref riidShellItem);
                        }
                        catch (InvalidCastException)
                        {
                            try
                            {
                                item = list.GetAt(i, ref riidShellLink);
                            }
                            catch (InvalidCastException)
                            {
                                item = null;
                            }
                        }
                        if (item == null)
                            continue;
                        string tmp = null;
                        if (item is API.IShellItem)
                        {
                            IntPtr ppIdl;
                            API.SHGetIDListFromObject(item, out ppIdl);
                            if (ppIdl != IntPtr.Zero)
                            {
                                var pwstr = IntPtr.Zero;
                                API.SHGetNameFromIDList(ppIdl, API.SIGDN.FILESYSPATH, ref pwstr);
                                if (pwstr != IntPtr.Zero)
                                {
                                    tmp = Marshal.PtrToStringUni(pwstr);
                                    Marshal.FreeCoTaskMem(pwstr);
                                }
                                Marshal.FreeCoTaskMem(ppIdl);
                            }
                        }
                        else
                        {
                            tmp = "::" + Util.ResolveLink(((API.IShellLink)item)).Item2;
                        }
                        if (!string.IsNullOrEmpty(tmp))
                            ret.Add(tmp);
                        Marshal.ReleaseComObject(item);
                    }
                    Marshal.ReleaseComObject(list);
                }
                Marshal.FinalReleaseComObject(listProvider);
                return ret;
            }
        }

        private static IEnumerable<string> GetP8Recent(string fsObject)
        {
            var o = fsObject.ToLowerInvariant();
            var l = P8JlImpl.Where(j => j.Arg == o && !string.IsNullOrEmpty(j.Cmd)).ToList();
            l.Sort();
            return from mfuElement in l 
                   select mfuElement.Cmd;
        }





        private class MfuElement : IComparable<MfuElement>
        {
            public string Arg;
            public string Cmd = "";
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
                       && !Arg.Contains("\",")
                       && !Arg.StartsWith("::")
                       && !Arg.StartsWith("\\\\")
                       && File.Exists(Arg);
            }

            public void Mix(MfuElement other)
            {
                LaunchCount += other.LaunchCount;
                if (LastLaunchTimeStamp < other.LastLaunchTimeStamp)
                    LastLaunchTimeStamp = other.LastLaunchTimeStamp;
            }

            

            public override int GetHashCode()
            {
                return (Arg + Cmd + LaunchCount + LastLaunchTimeStamp).GetHashCode();
            }

            public MfuElement Clone()
            {
                return new MfuElement
                           {
                               Arg = Arg,
                               Cmd = Cmd,
                               LaunchCount = LaunchCount,
                               LastLaunchTimeStamp = LastLaunchTimeStamp
                           };
            }

            public override bool Equals(object obj)
            {
                if (!(obj is MfuElement))
                    return false;
                return GetHashCode() == obj.GetHashCode();
            }
        }
    }
}

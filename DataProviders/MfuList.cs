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
using Power8.Helpers;
using Power8.Properties;

namespace Power8
{
    /// <summary>
    /// Provides list of Moset Frequently Used applications in the system
    /// </summary>
    public static class MfuList
    {
        private static ObservableCollection<PowerItem> _startMfu;
        /// <summary>
        /// Gets the bindable ObservableCollection of MFU items for Start Menu
        /// </summary>
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
        /// <summary>
        /// The search root for MFU. Used for different Search() methods from PITree.
        /// </summary>
        public static PowerItem MfuSearchRoot;
        /// <summary>
        /// The list of user exclusionf from MFU in P8 mode
        /// </summary>
        public static readonly ObservableCollection<StringWrapper> ExclList = new ObservableCollection<StringWrapper>();


        private static readonly List<MfuElement> LastList = new List<MfuElement>(); //The last checked state of MFU data
        private static readonly List<MfuElement> P8JlImpl = new List<MfuElement>(); //Power8's own JumpList items implementation
        private static readonly List<String> PinList = new List<string>();          //The list of pinned elements
        private static readonly List<String> UserList = new List<string>();         //The Custom MFU list 
        private static readonly string[] MsFilter;                                  //M$'s filer of file names that shan't be watched
        private static readonly int SessionId = Process.GetCurrentProcess().SessionId;  //Needed to check if new process was created in our session

        private static ManagementEventWatcher WatchDog;                             //Notifies when a process is created in system

        //Files with data
        private static readonly string DataBaseRoot = 
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Power8_Team\\";

        private static readonly string LaunchDB = DataBaseRoot + "LaunchData.csv",
                                       PinDB = DataBaseRoot + "PinData.csv",
                                       UserExclDB = DataBaseRoot + "Exclusions.csv",
                                       UserListDB = DataBaseRoot + "CustomList.csv";

        //Registry pathes
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


        /// <summary>
        /// Type constructor. Initializes MS exception list, P8 JL implementation list and Pinned list.
        /// Also creates WMI event watcher.
        /// </summary>
        static MfuList()
        {
            //Reading ms exclusion list
            var col = new List<String>();
            using(var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                   @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileAssociation", 
                   false))
            {
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
            }
// ReSharper restore AccessToDisposedClosure
            col.Add("APPLICATION SHORTCUTS");
            col.Add("HOST.EXE");
            col.Add("VSLAUNCHER.EXE");
            col.Add("SETUP64.EXE");
            MsFilter = col.ToArray();

            //Reading launch data
            if(File.Exists(LaunchDB))
            {
                using (var f = new StreamReader(LaunchDB, Encoding.UTF8))
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
                                                Arg = ls[0].Replace('/', '\\'),
                                                Cmd = ls[1],
                                                LaunchCount = int.Parse(ls[2]),
                                                LastLaunchTimeStamp =
                                                    DateTime.ParseExact(ls[3], "yyyyMMddHHmmss", CultureInfo.CurrentCulture)
                                            });
                    }
                }
            }

            //Reading pin data
            if (File.Exists(PinDB))
            {
                using (var f = new StreamReader(PinDB, Encoding.UTF8))
                {
                    while (!f.EndOfStream)
                    {
                        var l = f.ReadLine();
                        if (string.IsNullOrEmpty(l))
                            continue;
                        PinList.Add(l);
                    }
                }
            }

            //Reading exclusions
            if (File.Exists(UserExclDB))
            {
                using (var f = new StreamReader(UserExclDB, Encoding.UTF8))
                {
                    while (!f.EndOfStream)
                    {
                        var l = f.ReadLine();
                        if (string.IsNullOrEmpty(l))
                            continue;
                        ExclList.Add(new StringWrapper {Value = l});
                    }
                }
            }

            //Reading user mfu list
            if (File.Exists(UserListDB))
            {
                using (var f = new StreamReader(UserListDB, Encoding.UTF8))
                {
                    while (!f.EndOfStream)
                    {
                        var l = f.ReadLine();
                        if (string.IsNullOrEmpty(l))
                            continue;
                        UserList.Add(l);
                    }
                }
            }

            //Save all on shutdown
            Util.MainDisp.ShutdownStarted += MainDispOnShutdownStarted;

            //Create COM object on main thread to prevent unexpected problems with stopping
            Util.Post(() =>
                          {
                              //React on new processes creation
                              WatchDog =
                                  new ManagementEventWatcher(
                                      "SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'");
                              WatchDog.EventArrived += WatchDogOnEventArrived;
                              WatchDog.Start();
                          });

        }
        
        // Save data on close
        private static void MainDispOnShutdownStarted(object sender, EventArgs eventArgs)
        {
            WatchDog.Stop();

            Directory.CreateDirectory(DataBaseRoot);

            //Writing Launch list
            using (var f = new StreamWriter(LaunchDB, false, Encoding.UTF8))
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

            //Writing pin list
            using (var f = new StreamWriter(PinDB, false, Encoding.UTF8))
            {
                foreach (var pinned in PinList)
                {
                    f.WriteLine(pinned);
                }
            }

            //Writing exclusions list
            using (var f = new StreamWriter(UserExclDB, false, Encoding.UTF8))
            {
                foreach (var excl in ExclList.Where(ex => !string.IsNullOrWhiteSpace(ex.Value)))
                {
                    f.WriteLine(excl.Value);
                }
            }

            //Writing custom mfu list
            using (var f = new StreamWriter(UserListDB, false, Encoding.UTF8))
            {
                foreach (var item in UserList)
                {
                    f.WriteLine(item);
                }
            }

        }

        //New process is created in system
        private static void WatchDogOnEventArrived(object sender, EventArrivedEventArgs e)
        {
            var proc = e.NewEvent["TargetInstance"] as ManagementBaseObject;
            if (proc == null) //The instance is available?
                return;
            var sId = (int)(uint) proc["SessionId"];
            if (sId != SessionId) //It is in our session?
                return;
            var parentPid = (int) (uint) proc["ParentProcessId"];
            if(parentPid != 0) //Parent available
            {
                try
                {
                    var parentProcess = Process.GetProcessById(parentPid);
                    if(parentProcess.SessionId != SessionId) //And parent is in our session?
                        return;
                }
                catch (InvalidOperationException)
                {
                    return; //msdn: "process was not started by this object", probably means SID is different
                }
                catch(ArgumentException){} //parent exited... well, assuming it was from our session
            }
            var cmd = (string) proc["CommandLine"];
            if(!string.IsNullOrEmpty(cmd) && !MsFilter.Any(cmd.ToUpper().Contains))
            {//And command line doesn't include filtered elements?
                var pair = Util.CommandToFilenameAndArgs(cmd);
                if (string.IsNullOrEmpty(pair.Item1) || pair.Item1.Length < 2 || pair.Item1[1] != ':' || !File.Exists(pair.Item1))
                    return; //As a rule, user-launched applications have full path. Something as "rundll %1 %2 %3" won't make sence for P8
                pair = Tuple.Create(pair.Item1.ToLowerInvariant(), pair.Item2.ToLowerInvariant());
                //Quotes make troubles in case it's a filename. Strip them in this case. 
                string checkPath = pair.Item2; 
                bool exists = File.Exists(checkPath);
                if (!exists && checkPath.StartsWith("\"") && checkPath.EndsWith("\""))
                {
                    checkPath = checkPath.Substring(1, checkPath.Length - 2);
                    exists = File.Exists(checkPath);
                    if(!exists)
                        checkPath = pair.Item2; //But since it's not a file, we'll leave quotes
                }
                var prefix = exists ? string.Empty : "::"; //If file doesn't exist, add a prefix to show that this JL item is a command
                pair = Tuple.Create(pair.Item1.ToLowerInvariant(), prefix + checkPath.ToLowerInvariant());
                //Add to jump list or update the launch data
                lock (P8JlImpl)
                {
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
                }
#if DEBUG
                Debug.WriteLine("Process Launched: {0} {1}", pair.Item1, pair.Item2);
#endif
            }
        }


        /// <summary>
        /// Starts asynchronous update of MFU list
        /// </summary>
        public static void UpdateStartMfu()
        {
            Util.Fork(UpdateStartMfuSync, "Update MFU").Start();
        }

        /// <summary>
        /// Synchronously updates the MFU list
        /// </summary>
        public static void UpdateStartMfuSync()
        {
            //Step 1: parse registry
            var list = SettingsManager.Instance.MfuIsSystem
                           ? GetMfuFromUserAssist()
                           : SettingsManager.Instance.MfuIsInternal
                                 ? GetMfuFromP8JL()
                                 : GetMfuFromCustomData();

            if (list.SequenceEqual(LastList)) //Exit if list  not changed comparing to the last one
                return;
            
            //Copy to the last list
            LastList.Clear();
            list.ForEach(m => LastList.Add(m.Clone()));

            lock (StartMfu) //Update the Start MFU
            {
                //Steps 2.1 - 2.4
                list.ApplyFiltersAndSort();

                //Step 2.6: limit single/zero-used to 20 items
                for (int i = 0, j = 0; i < list.Count; i++)
                {
                    if (list[i].LaunchCount<2 && ++j > 20)
                    {
                        list.RemoveRange(i, list.Count - i);
                        break;
                    }
                }


                //Step 3: update collection
                Util.Send(() =>
                              {
                                  _startMfu.Clear();
                                  foreach (var mfuElement in list)
                                  {
                                      var elem = mfuElement;
                                      _startMfu.Add(new PowerItem
                                        {
                                            Argument = elem.Arg,
                                            Parent = MfuSearchRoot,
                                            AllowAsyncFriendlyName = true, //Friendly Name isn't distinctive for MFU
                                            IsPinned = elem.LaunchCount >= 2000, //hack yeah!
                                            IsFolder = SettingsManager.Instance.MfuIsCustom 
                                                        && Directory.Exists(elem.Arg)
                                        });
                                  }
                              });
            }
        }

        /// <summary>
        /// Gets list of most frequently used data from User Assist registry keys
        /// </summary>
        private static List<MfuElement> GetMfuFromUserAssist()
        {
            var list = new List<MfuElement>();
            string ks1, ks2; //reg key strings
            int dataWidthExpected, fileTimeOffset, launchCountCorrection; //key parameters
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
            if (k1 != null && k2 != null)
            {
                foreach (var k in new[] { k1, k2 })
                {
                    var ak = k;
                    list.AddRange(
                        (from valueName in ak.GetValueNames()
                         let data = (byte[])ak.GetValue(valueName)
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
            return list;
        }
        
        /// <summary>
        /// Gets list of most frequently used data from Power8 JL data
        /// </summary>
        private static List<MfuElement> GetMfuFromP8JL()
        {
            var list = new Dictionary<string, MfuElement>();
            lock (P8JlImpl)
            {
                foreach (var mfuElement in P8JlImpl.Where(mfu => mfu.IsOk()))
                {
                    var k = mfuElement.Arg;
                    if (ExclList.Any(ex => !string.IsNullOrWhiteSpace(ex.Value) && k.Contains(ex.Value)))
                        continue;
                    if (list.ContainsKey(k))
                        list[k].Mix(mfuElement);
                    else
                        list[k] = mfuElement.Clone();
                }
            }
            return list.Select(kv => kv.Value).ToList();
        }

        /// <summary>
        /// Gets list of most frequently used data from user's customized list
        /// </summary>
        private static List<MfuElement> GetMfuFromCustomData()
        {
            var res = new List<MfuElement>();
            for (int i = UserList.Count - 1; i >= 0; i--)
            {
                res.Add(new MfuElement {Arg = UserList[i], LaunchCount = i});
            }
            return res;
        }

        /// <summary>
        /// Updates JumpList of PowerItem passed asynchronously. JumpList is being built based on
        /// - system Frequent list (W7+);
        /// - system Recent list (W7+);
        /// - Power8 internal jumplist (WXP+);
        /// </summary>
        /// <param name="item">the PowerItem whose JumpList may be updated</param>
        public static void GetRecentListFor(PowerItem item)
        {
            Util.Fork(() => GetRecentListForSync(item), "MFU worker for " + item.Argument).Start();
        }
        //Same as above but inSync
        private static void GetRecentListForSync(PowerItem item)
        {
            string fsObject;
            try
            {
                fsObject = PowerItemTree.GetResolvedArgument(item);
            }
            catch (IOException) //No money no honey (no object to get JL for)
            {
                return;
            }
            IEnumerable<string> jl = null;
            var p8R = GetP8Recent(item.IsLink ? item.ResolvedLink : fsObject); //P8 internal JL
            if(Util.OsIs.SevenOrMore) //System JL
            {
                var recent = GetJumpList(fsObject, API.ADLT.RECENT);
                var frequent = GetJumpList(fsObject, API.ADLT.FREQUENT);
                if (recent != null && frequent != null)
                    jl = recent.Union(frequent);
                else
                    jl = recent ?? frequent;
            } 
            if (jl != null && p8R != null) //merge everything
                jl = jl.Union(p8R);
            else
                jl = jl ?? p8R;
            if (jl == null) 
                return; //No jump list discovered -> nothing to do
            jl = jl.Distinct()                                          //No duplicates
                   .Where(x => x.StartsWith("::") || File.Exists(x))    //No obsoletes
                   .Take(25);                                           //Not too many
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

        /// <summary>
        /// Updates internal pin data based on already updated PowerItem
        /// </summary>
        /// <param name="item">PowerItem, whose IsPinned property is already set to desired value</param>
        public static void PinUnpin(PowerItem item)
        {
            bool update = false;
            if (item.IsPinned && !PinList.Contains(item.Argument))
            {//Item was pinned
                PinList.Add(item.Argument);
                update = true;
            }
            else if ((!item.IsPinned) && PinList.Contains(item.Argument))
            {//Item was unpinned
                PinList.Remove(item.Argument);
                update = true;
            }
            //else means state of item may changed but already reflected in pin list
            if (!update) 
                return;
            //Calculate the new index
            var temp = new List<MfuElement>();
            LastList.ForEach(m => temp.Add(m.Clone()));
            temp.ApplyFiltersAndSort();
            //Destination index. When moving data from LastList to StartMFU, it is truncated to contain
            //only 20 items with launchCount==0. So it is possible that when you unpin an element,
            //it's calculated position in scope of LastList will be more that StartMFU contains.
            //This is the fix for the problem.
            var tIdx = Math.Min(temp.FindIndex(mfu => mfu.Arg == item.Argument), StartMfu.Count - 1);
            StartMfu.Move(StartMfu.IndexOf(item), tIdx);
        }
        /// <summary>
        /// Adds an exclusion that hides MFU items that fall under it.
        /// Exclusions work when MFU is in P8 mode.
        /// </summary>
        /// <param name="exclusion">text to exclude from display. 
        /// When tested for match, used with ** on both sides</param>
        public static void AddExclusion(PowerItem exclusion)
        {
            ExclList.Add(new StringWrapper {Value = exclusion.Argument.ToLower()});
            UpdateStartMfuSync();
        }

        public static void Add2Custom(PowerItem item, string fullPath = null)
        {
            var arg = item == null ? fullPath : PowerItemTree.GetResolvedArgument(item);
            var idx = UserList.IndexOf(arg);
            if(idx == UserList.Count -1 && UserList.Count > 0) //Already in list and top item
                return;
            if(idx != -1) //Already in list, move to top
                UserList.RemoveAt(idx);
            UserList.Add(arg); //Including case where item not in list
            if(SettingsManager.Instance.MfuIsCustom && fullPath == null)
                UpdateStartMfuSync(); //Refresh
        }

        public static void RemoveCustom(PowerItem item)
        {
            UserList.Remove(PowerItemTree.GetResolvedArgument(item));
            UpdateStartMfuSync();
        }

        public static int MoveCustomListItem(PowerItem which, PowerItem where)
        {
            //1. Change order in UserList 
            var argFrom = PowerItemTree.GetResolvedArgument(which);
            var argTo = PowerItemTree.GetResolvedArgument(@where);
            int idxFrom = UserList.IndexOf(argFrom);
            int idxTo = UserList.IndexOf(argTo);
            UserList.RemoveAt(idxFrom);
            UserList.Insert(idxTo + idxFrom > idxTo ? 1 : 0, argFrom);
            //2. Update LastList
            LastList.Clear();
            GetMfuFromCustomData().ForEach(m => LastList.Add(m.Clone()));
            //3. Change order in MfuList - with respect to pinning
            StartMfu.Move(StartMfu.IndexOf(which), StartMfu.IndexOf(where));
            return StartMfu.IndexOf(which);
        }


        /// <summary>
        /// Implementation of Rot13 "encryption" algorythm used in Windows to "protect" user data
        /// </summary>
        /// <param name="s">String that shall be decoded or encoded</param>
        /// <returns>String that is decoded or encoded</returns>
        private static string Rot13(string s)
        {
            var r13 = new StringBuilder(s.Length);
            var bytes = Encoding.ASCII.GetBytes(s);
            var a = Encoding.ASCII.GetBytes("A")[0];
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {//a pieace of dark magic...
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
        /// <summary>
        /// This fumction translates Windows UserAssist data entries into readable data
        /// </summary>
        /// <param name="s">Rot13-encoded UserAssist string data</param>
        /// <returns>Decoded string with expanded known folder guids, special folder IDs and stripped off UEME_-prefixes</returns>
        private static string DeRot13AndKnwnFldr(string s)
        {
            s = Rot13(s);
            if(s.StartsWith("{")) //KnownFolder. Expanding...
            {
                var pair = s.Split(new[] {'}'}, 2);
                return Util.ResolveKnownFolder(pair[0].Substring(1)) + pair[1];
            }
            if (s.StartsWith("UEME_"))//Prefix. Removing...
            {
                s = s.Split(new[] {':'}, 2)[1];
            }
            if (s.StartsWith("%csidl"))//SpecialFolder ID. Expanding...
            {
                var pair = s.Split(new[] {'%'}, 2, StringSplitOptions.RemoveEmptyEntries);
                s = Util.ResolveSpecialFolder((API.Csidl) int.Parse(pair[0].Substring(5))) + pair[1];
            }
            return s;
        }
        /// <summary>
        /// Filters out MFU elements that:
        /// - are in MS Filter list;
        /// - contain double slashes;
        /// Then joins objects and links to theese objects prewfering the links,
        /// increases usage to visualize pinning,
        /// and finally sorts the collection.
        /// </summary>
        /// <param name="list">Collection of MFU elements</param>
        private static void ApplyFiltersAndSort(this List<MfuElement> list )
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
            //Step 2.3.1 : removing duplicate links
            for (int i = linx.Count - 1; i > 0; i--)
            {
                if (linx[i - 1].Item1.Equals(linx[i].Item1, StringComparison.InvariantCultureIgnoreCase))
                {
                    linx[i - 1].Item2.Mix((linx[i].Item2));
                    if (Util.OsIs.SevenOrMore && linx[i].Item2.Arg.Contains("TaskBar"))
                        linx[i - 1].Item2.Arg = linx[i].Item2.Arg;//priority for taskbar elements
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

            //Step 2.3.3: apply pinning
            foreach (var mfuElement in list.Where(mfuElement => PinList.Contains(mfuElement.Arg)))
            {
                mfuElement.LaunchCount += 2000; //simply popping these up
            }

            //Step 2.4: sort.
            list.Sort();

            //Step 2.5: remove duplicates based on args (yes, this is possible even after we filtered out so many items)
            for (int i = 0; i < list.Count - 1; i++)//The top index, used to select main item
            {
                for (int j = list.Count - 1; j > i; j--) //bottom index, selects duplicate
                {
                    if (list[i].Arg.Equals(list[j].Arg, StringComparison.OrdinalIgnoreCase))
                    {//duplicate found
                        list[i].Mix((list[j])); //mix less-popular into more popular
                        list.RemoveAt(j);       //remove less popular
                    }
                }
            }
        }
        //Excludes the elements that are like ones to be filtered out using M$ User Assist filter
        private static void ApplyMsFilter(this List<MfuElement> list )
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var ucase = list[i].Arg.ToUpperInvariant();
                if(MsFilter.Any(ucase.Contains))
                    list.RemoveAt(i);
            }
        }
        /// <summary>
        /// Gets system jump list of a desired type for a file system object given
        /// using the property store approach as the only one possible when getting the JL 
        /// for object other than your own application
        /// </summary>
        /// <param name="fsObject">file system object which we need jump list for</param>
        /// <param name="listType">the type of a jump list - rercent or frequent</param>
        /// <returns>the IEnumerable of strings representing the JumpList: 
        /// FS pathes for files, "::" + command lines for target links</returns>
        private static IEnumerable<string> GetJumpList(string fsObject, API.ADLT listType)
        {
            //Getting the property store
            var riidPropertyStore = new Guid(API.Sys.IdIPropertyStore);
            API.IPropertyStore store;
            var res = API.SHGetPropertyStoreFromParsingName(fsObject,
                                                            IntPtr.Zero,
                                                            API.GETPROPERTYSTOREFLAGS.GPS_DEFAULT,
                                                            ref riidPropertyStore,
                                                            out store);
            if (res > 0)
                return null;
            //Getting the AppUserModelId
            using (var pv2 = new API.PROPVARIANT())
            {
                store.GetValue(API.PKEY.AppUserModel_ID, pv2);
                Marshal.FinalReleaseComObject(store);
                if (pv2.longVal == 0)
                    return null;

                //Getting the required list
                var ret = new Collection<string>();
                var listProvider = (API.IApplicationDocumentLists)new API.ApplicationDocumentLists();
                listProvider.SetAppID(pv2.GetValue());
                var riidObjectArray = new Guid(API.Sys.IdIObjectArray);
                API.IObjectArray list;
                try
                {
                    list = (API.IObjectArray)listProvider.GetList(listType, 0, ref riidObjectArray);
                }
                catch (COMException) //Share violation may occur
                {
                    list = null;
                }
                
                if (list != null)
                {
                    //Getting the contents of the list
                    var riidShellItem = new Guid(API.Sys.IdIShellItem);
                    var riidShellLink = new Guid(API.Sys.IdIShellLinkW);
                    for (uint i = 0; i < list.GetCount(); i++)
                    {
                        object item;
                        try //we don't know what is inside - so let's use exceptions from IDispatch
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
                        if (item is API.IShellItem) //get the FILEPATH from IShellItem
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
                        else //get command from IShellLink
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
        /// <summary>
        /// Gets P8's internal JumpList for given object
        /// </summary>
        /// <param name="fsObject">file system object which we need recently used items for</param>
        /// <returns>the IEnumerable of strings representing the JumpList: 
        /// FS pathes for files, "::" + command lines for commands of a different kind</returns>
        private static IEnumerable<string> GetP8Recent(string fsObject)
        {
            var o = fsObject.ToLowerInvariant();
            List<MfuElement> l;
            lock (P8JlImpl)
            {
                l = P8JlImpl.Where(j => j.Arg == o
                                        && !string.IsNullOrEmpty(j.Cmd)
                                        && j.Cmd != "::").ToList();
                                                  // ^^-actually means "no command"
            }
            l.Sort();
            return from mfuElement in l 
                   select mfuElement.Cmd;
        }


        /// <summary>
        /// Represents one entry in different MFU lists - P8 JL implementation and system UserAssist data
        /// </summary>
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
                    : -LaunchCount.CompareTo(other.LaunchCount); //"-" because sort descending
            }
            /// <summary>
            /// Checks if the constructed MFU elements contains valid data
            /// </summary>
            public bool IsOk()
            {
                return LaunchCount > -1
                       && LastLaunchTimeStamp != DateTime.MinValue
                       && !Arg.Contains("\",")
                       && !Arg.StartsWith("::")
                       && !Arg.StartsWith("\\\\")
                       && File.Exists(Arg);
            }
            /// <summary>
            /// Mixes two MfuElements into one. The instance of this method is the resulting target.
            /// Mixing means summing the launch count and updating the last launch time stamp.
            /// </summary>
            /// <param name="other">2nd MfuElement to mix</param>
            public void Mix(MfuElement other)
            {
                LaunchCount += other.LaunchCount;
                if (LaunchCount >= 4000)
                    LaunchCount -= 1999; //if both items were identified as pinned, simply add one, in any other case mix values
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

        /// <summary>
        /// Wraps string exclusion so list of it can be bound to datagrid
        /// </summary>
        public class StringWrapper
        {
            public string Value { get; set; }
        }
    }
}

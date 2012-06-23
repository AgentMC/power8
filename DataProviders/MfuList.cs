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
        
        public static void UpdateStartMfu()
        {
            var sss = System.Diagnostics.Stopwatch.StartNew();
            //Step 1: parse registry
            var list = new List<MfuElement>();
            var k1 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist\{CEBFF5CD-ACE2-4F4F-9178-9926F41749EA}\Count",
                false);
            var k2 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist\{F4E57C4B-2036-45F0-A9AB-443BCFE33D9F}\Count",
                false);
            if(k1 != null && k2 != null)
            {
                foreach (var k in new[]{k1, k2})
                {
                    list.AddRange(from mfu in
                                      (from valueName in k.GetValueNames()
                                       where valueName.Contains("\\")
                                       let data = (byte[]) k.GetValue(valueName)
                                       select new MfuElement
                                                  {
                                                      Arg = DeRot13AndKnwnFldr(valueName),
                                                      LaunchCount = BitConverter.ToInt32(data, 4),
                                                      LastLaunchTimeStamp =
                                                          DateTime.FromFileTime(BitConverter.ToInt64(data, 60))
                                                  })
                                  where mfu.IsOk()
                                  select mfu);
                }
                k1.Close();
                k2.Close();
            }

            //Step 2: filter and sort
            var linx = list
                .FindAll(m => m.Arg.EndsWith(".lnk", StringComparison.InvariantCultureIgnoreCase))
                .Select(l => Util.ResolveLink(l.Arg)).ToArray();
            list.RemoveAll(l => linx.Contains(l.Arg, StringComparer.InvariantCultureIgnoreCase));
            list.Sort();
            
            //Step 3: update collection
            lock (_startMfu)
            {
                _startMfu.Clear();
                foreach (var mfuElement in list)
                {
                    _startMfu.Add(new PowerItem
                                      {
                                          Argument = mfuElement.Arg,
                                      });
                }
            }
            sss.Stop();
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
            return s;
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
                return LaunchCount > 0
                       && LastLaunchTimeStamp != DateTime.MinValue
                       && System.IO.File.Exists(Arg);
            }
        }
    }
}

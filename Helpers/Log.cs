using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Power8.Helpers
{
    public static class Log
    {
        [Conditional("DEBUG")]
        public static void Raw(string message, string obj = null)
        {
            var b = new StringBuilder(DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss.ffff\t"));
            var mtd = new StackTrace().GetFrame(obj == string.Empty ? 2 : 1).GetMethod();
            var loc = mtd.DeclaringType == null ? "global" : mtd.DeclaringType.Name;
            b.AppendFormat("{0}\t{1}::{2}", Thread.CurrentThread.ManagedThreadId, loc, mtd.Name);
            if(!string.IsNullOrEmpty(obj))
                b.Append(" for " + obj);
            b.Append("\t" + message);
            Debug.WriteLine(b.ToString());
        }

        [Conditional("DEBUG")]
        public static void Fmt(string format, params object[] args)
        {
            Raw(string.Format(format, args), string.Empty);
        }
    }
}

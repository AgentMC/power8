using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Power8.Helpers
{
    public static class Log
    {
        /// <summary>
        /// Logs message passed to the current debug log.
        /// Prepends calling method name, date, time and thread id.
        /// Optionally adds info on object which the callee was invoked to serve for.
        /// </summary>
        /// <param name="message">Text to log</param>
        /// <param name="obj">Description or name of the object calling method is working on.
        /// If the method doesn't serve any particular object at the moment, leave it unset.</param>
        [Conditional("DEBUG")]
        public static void Raw(string message, string obj = null)
        {
            var b = new StringBuilder(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.ffff\t"));
            var mtd = new StackTrace().GetFrame(obj == string.Empty ? 2 : 1).GetMethod();
            var loc = mtd.DeclaringType == null ? "global" : mtd.DeclaringType.Name;
            b.AppendFormat("{0}\t{1}::{2}", Thread.CurrentThread.ManagedThreadId, loc, mtd.Name);
            if(!string.IsNullOrEmpty(obj))
                b.Append(" for " + obj);
            b.Append("\t" + message);
            Debug.WriteLine(b.ToString());
        }
        /// <summary>
        /// Logs message passed to the current debug log.
        /// Prepends calling method name, date, time and thread id.
        /// </summary>
        /// <param name="format">Format string as used by String.Format().</param>
        /// <param name="args">Format string args as used by String.Format().</param>
        [Conditional("DEBUG")]
        public static void Fmt(string format, params object[] args)
        {
            Raw(string.Format(format, args), string.Empty);
        }
    }
}

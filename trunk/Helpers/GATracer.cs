using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Power8.Helpers
{
    public class GATracer: Exception
    {
        private readonly int _id;
        private readonly Exception _original;
        private readonly Dictionary<string, string> _data;
        private GATracer(int id, Exception original, Dictionary<string, string> traceData)
        {
            _id = id;
            _original = original;
            _data = traceData;
        }

        public override string ToString()
        {
            var b = new StringBuilder("TID:");
            b.Append(_id).Append(".");
            foreach (var data in _data)
            {
                b.AppendFormat("{0}:{1};", data.Key, data.Value);
            }
            b.Append("@");
            int pos = 0, len = 0;
            bool first = true;
            var orig = _original.ToString();
            using (var r = new StringReader(orig))
            {
                do
                {
                    var l = r.ReadLine() ?? string.Empty;
                    if (l.Contains("Power8"))
                        break;
                    if (first)
                        first = false;
                    else
                        pos += len;//position of PRE-last line
                    len = l.Length; //length of LAST line
                    len += Environment.NewLine.Length;
                } while (r.Peek() > -1);
            }
            b.Append(orig.Substring(pos)).Append(_original);
            return b.ToString();
        }

        public static void PostTraceData(int id, Exception original, Dictionary<string, string> traceData)
        {
            Analytics.PostException(new GATracer(id, original, traceData), true);
        }
    }
}

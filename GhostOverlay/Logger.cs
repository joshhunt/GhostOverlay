using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostOverlay
{
    public delegate void LogFn(string msg);

    class Logger
    {
        private static int maxPrefixLength = 0;

        public static LogFn MakeLogger(string prefix)
        {
            maxPrefixLength = Math.Max(maxPrefixLength, prefix.Count());

            return msg =>
            {
                var padding = Math.Max(0, maxPrefixLength - prefix.Count()) + 2;

                var output = $"[{prefix}]{new string(' ', padding)}{msg}";
                Debug.WriteLine(output);
            };
        }
    }
}

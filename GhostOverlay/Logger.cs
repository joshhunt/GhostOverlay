using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using Serilog;
using Serilog.Events;

namespace GhostOverlay
{
    public delegate void LogFn(string msg);

    class Logger
    {
        private static int maxPrefixLength = 0;
        private static Serilog.Core.Logger sLog;
        private static int ONE_MEGABYTE = 1000000;

        private string prefix;

        public Logger(string _prefix)
        {
            prefix = _prefix;
            maxPrefixLength = Math.Max(maxPrefixLength, prefix.Count());

            if (sLog == null)
            {
                var logLocation = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "log.txt");

                sLog = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Debug()
                    .WriteTo.File(logLocation, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Debug, fileSizeLimitBytes: ONE_MEGABYTE, retainedFileCountLimit: 5)
                    .CreateLogger();
            }
        }

        private string makeMessage(string message)
        {
            var padding = Math.Max(0, maxPrefixLength - prefix.Count()) + 2;
            return $"[{prefix}]{new string(' ', padding)}{message}";
        }

        public void Info(string message)
        {
            sLog.Information(makeMessage(message));
        }

        public void Info<T1>(string message, T1 arg1)
        {
            sLog.Information(makeMessage(message), arg1);
        }

        public void Info<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            sLog.Information(makeMessage(message), arg1, arg2);
        }

        public void Info<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            sLog.Information(makeMessage(message), arg1, arg2, arg3);
        }

        public void Debug(string message)
        {
            sLog.Debug(makeMessage(message));
        }

        public void Debug<T1>(string message, T1 arg1)
        {
            sLog.Debug(makeMessage(message), arg1);
        }

        public void Error(string message)
        {
            sLog.Error(makeMessage(message));
        }

        public void Error(string message, Exception err)
        {
            sLog.Error(makeMessage(message), err);
        }
    }
}

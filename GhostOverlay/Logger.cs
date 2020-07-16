using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarSymbols;
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
        private static readonly List<string> exclusives = new List<string>();

        private string prefix;

        private static void MakeSlog()
        {
            if (sLog == null)
            {
                var logLocation = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "log.txt");

                sLog = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Debug()
                    .WriteTo.File(logLocation, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information, fileSizeLimitBytes: ONE_MEGABYTE, retainedFileCountLimit: 5)
                    .CreateLogger();
            }
        }

        public Logger(string _prefix, bool exclusive = false)
        {
            MakeSlog();
            prefix = _prefix;
            maxPrefixLength = Math.Max(maxPrefixLength, prefix.Count());

            if (exclusive)
            {
                exclusives.Add(prefix);
            }
        }

        private bool shouldLog()
        {
            return exclusives.Count == 0 || exclusives.Contains(prefix);
        }

        private string makeMessage(string message)
        {
            var padding = Math.Max(0, maxPrefixLength - prefix.Count()) + 2;
            return $"[{prefix}]{new string(' ', padding)}{message}";
        }

        public void Info(string message)
        {
            if (shouldLog()) sLog.Information(makeMessage(message));
        }

        public void Info<T1>(string message, T1 arg1)
        {
            if (shouldLog()) sLog.Information(makeMessage(message), arg1);
        }

        public void Info<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            if (shouldLog()) sLog.Information(makeMessage(message), arg1, arg2);
        }

        public void Info<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            if (shouldLog()) sLog.Information(makeMessage(message), arg1, arg2, arg3);
        }


        public void Info<T1, T2, T3, T4>(string message, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (shouldLog()) sLog.Information(makeMessage(message), arg1, arg2, arg3, arg4);
        }

        public void Debug(string message)
        {
            if (shouldLog()) sLog.Debug(makeMessage(message));
        }

        public void Debug<T1>(string message, T1 arg1)
        {
            if (shouldLog()) sLog.Debug(makeMessage(message), arg1);
        }

        public void Debug<T1, T2>(string message, T1 arg1, T2 arg2)
        {
            if (shouldLog()) sLog.Debug(makeMessage(message), arg1, arg2);
        }

        public void Debug<T1, T2, T3>(string message, T1 arg1, T2 arg2, T3 arg3)
        {
            if (shouldLog()) sLog.Debug(makeMessage(message), arg1, arg2, arg3);
        }

        public void Debug<T1, T2, T3, T4>(string message, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (shouldLog()) sLog.Debug(makeMessage(message), arg1, arg2, arg3, arg4);
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

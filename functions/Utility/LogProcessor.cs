using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KnowShow.ViewModel;
using static KnowShow.Repository.LogStore;

namespace functions.Utility
{
    public static class LogProcessor
    {
        public static IEnumerable<LogItem> SuccessByContains(this IList<LogStoreItem> logItems, string successText)
        {
            return logItems.Select(logItem => logItem.SuccessByContains(successText));
        }

        public static LogItem SuccessByContains(this LogStoreItem logItem, string successText)
        {
            return new LogItem(
                timestamp: logItem.Timestamp,
                result: logItem.Result,
                successful: logItem.Result.ToLowerInvariant().Contains(successText)
            );
        }

        public static IEnumerable<LogItem> SuccessByPattern(this IList<LogStoreItem> logItems, Regex regex)
        {
            return logItems.Select(logItem => logItem.SuccessByPattern(regex));
        }

        public static LogItem SuccessByPattern(this LogStoreItem logItem, Regex regex)
        {
            return new LogItem(
                timestamp: logItem.Timestamp,
                result: logItem.Result,
                successful: regex.IsMatch(logItem.Result)
            );
        }
    }
}
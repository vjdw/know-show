using KnowShow.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static KnowShow.Repository.Entities.LogStore;

namespace KnowShow.Utility
{
    public static class LogProcessor
    {
        public static IEnumerable<LogItemDto> SuccessByContains(this IEnumerable<LogStoreItem> logItems, string successText)
        {
            return logItems.Select(logItem => logItem.SuccessByContains(successText));
        }

        public static LogItemDto SuccessByContains(this LogStoreItem logItem, string successText)
        {
            return new LogItemDto(
                timestamp: logItem.Timestamp,
                result: logItem.Result,
                successful: logItem.Result.ToLowerInvariant().Contains(successText.ToLowerInvariant())
            );
        }

        public static IEnumerable<LogItemDto> SuccessByPattern(this IEnumerable<LogStoreItem> logItems, Regex regex)
        {
            return logItems.Select(logItem => logItem.SuccessByPattern(regex));
        }

        public static LogItemDto SuccessByPattern(this LogStoreItem logItem, Regex regex)
        {
            return new LogItemDto(
                timestamp: logItem.Timestamp,
                result: logItem.Result,
                successful: regex.IsMatch(logItem.Result)
            );
        }
    }
}
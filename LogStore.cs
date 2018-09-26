using System;
using System.Collections.Generic;

namespace KnowShow
{
    public class LogStore
    {
        public LogStore(string name)
        {
            Name = name;
            Logs = new List<LogItem>();
        }

        public string Name { get; set; }

        public List<LogItem> Logs { get; set; }

        public class LogItem
        {
            public LogItem(DateTime timestamp, string result)
            {
                Timestamp = timestamp;
                Result = result;
            }
            public DateTime Timestamp {get; private set;}
            public string Result {get; private set;}
            
        }
    }
}
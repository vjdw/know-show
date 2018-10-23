using System;
using System.Collections.Generic;

namespace KnowShow.Repository
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
            public LogItem(DateTime timestamp, bool successful, string result)
            {
                Timestamp = timestamp;
                Successful = successful;
                Result = result;
            }
            public DateTime Timestamp {get; private set;}
            public bool Successful {get; private set;}
            public string Result {get; private set;}
            
        }
    }
}
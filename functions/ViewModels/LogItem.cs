using System;
using System.Collections.Generic;

namespace KnowShow.ViewModel
{
    public class LogItem
    {
        public LogItem(DateTime timestamp, string result, bool successful)
        {
            Timestamp = timestamp;
            Result = result;
            Successful = successful;
        }
        public DateTime Timestamp { get; private set; }
        public string Result { get; private set; }
        public bool Successful { get; private set; }
    }
}
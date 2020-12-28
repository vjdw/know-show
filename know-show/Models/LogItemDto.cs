using System;
using System.Collections.Generic;

namespace KnowShow.Models
{
    public class LogItemDto
    {
        public LogItemDto(DateTime timestamp, string result, bool successful)
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
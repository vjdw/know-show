using System;
using System.Collections.Generic;

namespace KnowShow.Repository.Entities
{
    public class LogStore
    {
        public LogStore(string name, string description, int displayOrder = 99999, int periodHours = 25)
        {
            Name = name;
            DisplayName = description;
            DisplayOrder = displayOrder;
            PeriodHours = periodHours;
            SuccessPattern = "";
            Logs = new List<LogStoreItem>();
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int DisplayOrder { get; set; }
        public string SuccessPattern { get; set; }
        public int PeriodHours { get; set; }
        public List<LogStoreItem> Logs { get; set; }

        public class LogStoreItem
        {
            public LogStoreItem(DateTime timestamp, string result)
            {
                Timestamp = timestamp;
                Result = result;
            }
            public DateTime Timestamp { get; private set; }
            public string Result { get; private set; }
        }
    }
}
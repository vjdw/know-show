using System;
using System.Collections.Generic;

namespace KnowShow.ViewModel
{
    public class LogViewModel
    {
        public LogViewModel(string name, IEnumerable<LogItem> logItems)
        {
            Name = name;
            LogItems = logItems;
        }

        public string Name { get; set; }
        public IEnumerable<LogItem> LogItems { get; set; }
    }
}
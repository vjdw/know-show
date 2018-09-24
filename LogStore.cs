using System.Collections.Generic;

namespace KnowShow
{
    public class LogStore
    {
        public LogStore(string name)
        {
            Name = name;
            Logs = new List<string>();
        }

        public string Name { get; set; }

        public List<string> Logs { get; set; }
    }
}
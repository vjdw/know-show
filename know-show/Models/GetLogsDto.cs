using System;
using System.Collections.Generic;

namespace KnowShow.Models
{
    public class GetLogsDto
    {
        public GetLogsDto(string name, IEnumerable<LogItemDto> logItems)
        {
            Name = name;
            LogItems = logItems;
        }

        public string Name { get; set; }
        public IEnumerable<LogItemDto> LogItems { get; set; }
    }
}
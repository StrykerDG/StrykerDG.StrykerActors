using System;
using System.Collections.Generic;
using System.Text;

namespace StrykerDG.StrykerActors
{
    public class CacheEntry
    {
        public DateTimeOffset Retrieved { get; set; }
        public dynamic Data { get; set; }
    }
}

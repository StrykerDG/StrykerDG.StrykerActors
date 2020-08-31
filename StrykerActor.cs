using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrykerDG.StrykerActors
{
    public class StrykerActor : ReceiveActor
    {
        protected IServiceScopeFactory ServiceScopeFactory { get; set; }
        protected Dictionary<string, CacheEntry> Cache { get; set; }
        protected TimeSpan ValidDuration { get; set; }

        public StrykerActor(IServiceScopeFactory factory, TimeSpan duration)
        {
            ServiceScopeFactory = factory;
            ValidDuration = duration;
            Cache = new Dictionary<string, CacheEntry>();
        }
    }
}

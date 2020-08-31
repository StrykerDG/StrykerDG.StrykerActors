using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using StrykerDG.StrykerActors.Clockify.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrykerDG.StrykerActors.Clockify
{
    public class ClockifyActor : StrykerActor
    {
        public ClockifyActor(IServiceScopeFactory factory, TimeSpan duration) : base(factory, duration)
        {
            Receive<AskForTimeEntries>(GetTimeEntries);
        }

        private async void GetTimeEntries(AskForTimeEntries message)
        {

        }
    }
}

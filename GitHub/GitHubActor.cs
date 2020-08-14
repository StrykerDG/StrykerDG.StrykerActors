using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrykerDG.StrykerActors.GitHub
{
    public class GitHubActor : ReceiveActor
    {
        private IServiceScopeFactory ServiceScopeFactory { get; set; }

        public GitHubActor(IServiceScopeFactory factory)
        {
            ServiceScopeFactory = factory;
        }
    }
}

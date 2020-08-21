using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using StrykerDG.StrykerActors.GitHub.Messages;
using StrykerDG.StrykerServices.GitHubService;
using StrykerDG.StrykerServices.GitHubService.Models;
using StrykerDG.StrykerServices.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrykerDG.StrykerActors.GitHub
{
    public class GitHubActor : ReceiveActor
    {
        private IServiceScopeFactory ServiceScopeFactory { get; set; }

        public GitHubActor(IServiceScopeFactory factory)
        {
            ServiceScopeFactory = factory;

            Receive<AskForGitHubUserProfile>(GetUserProfile);
        }

        private async void GetUserProfile(AskForGitHubUserProfile message)
        {
            using (var scope = ServiceScopeFactory.CreateScope())
            {
                var service = scope.ServiceProvider.GetServices<IStrykerService>()
                    .Where(s => s.GetType() == typeof(GitHubService))
                    .FirstOrDefault();

                var result = await service?.Get($"users/{message.Profile}");

                var resultObject = JsonConvert.DeserializeObject<GitHubUser>((string)result.Data);
                Sender.Tell(resultObject);
            }
        }
    }
}

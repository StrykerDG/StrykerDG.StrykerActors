using Akka.Actor;
using Akka.Util.Internal;
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
    public class GitHubActor : StrykerActor
    {
        public GitHubActor(IServiceScopeFactory factory, TimeSpan duration) : base(factory, duration)
        {
            Receive<AskForGitHubUserProfile>(GetUserProfile);
        }

        private async void GetUserProfile(AskForGitHubUserProfile message)
        {
            Cache.TryGetValue("USER_PROFILE", out var cacheResponse);

            if (
                cacheResponse == null ||
                cacheResponse?.Retrieved.Add(ValidDuration) < DateTimeOffset.Now
            )
            {
                using (var scope = ServiceScopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetServices<IStrykerService>()
                        .Where(s => s.GetType() == typeof(GitHubService))
                        .FirstOrDefault();

                    var result = await service?.Get($"users/{message.Profile}");

                    var resultObject = JsonConvert.DeserializeObject<GitHubUser>((string)result.Data);

                    Cache.AddOrSet(
                        "USER_PROFILE",
                        new CacheEntry
                        {
                            Retrieved = DateTimeOffset.Now,
                            Data = resultObject
                        }
                    );

                    Sender.Tell(resultObject);
                }
            }
            else
                Sender.Tell(cacheResponse.Data as GitHubUser);
        }
    }
}

using Akka.Actor;
using Akka.Util.Internal;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using StrykerDG.StrykerActors.Clockify.Messages;
using StrykerDG.StrykerServices.ClockifyService;
using StrykerDG.StrykerServices.ClockifyService.Models;
using StrykerDG.StrykerServices.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            using (var scope = ServiceScopeFactory.CreateScope())
            {
                // First check if we have the required information in cache
                Cache.TryGetValue("USER_PROFILE", out var userCache);
                Cache.TryGetValue("PROJECTS", out var projectCache);

                var service = scope.ServiceProvider.GetServices<IStrykerService>()
                    .Where(s => s.GetType() == typeof(ClockifyService))
                    .FirstOrDefault();
                var now = DateTimeOffset.Now;

                // If we're missing the user we need to get that before continuing
                if (userCache == null)
                {
                    var userResult = await service?.Get("v1/user");
                    var userResultObject = userResult.Data != null
                        ? JsonConvert.DeserializeObject<ClockifyUser>((string)userResult.Data)
                        : null;

                    var userCacheEntry = new CacheEntry
                    {
                        Retrieved = now,
                        Data = userResultObject
                    };

                    userCache = userCacheEntry;
                    Cache.AddOrSet(
                        "USER_PROFILE",
                        userCacheEntry
                    );
                }

                // Now that we have the user and workspace ids, we can get the tags 
                // and time entries
                var userData = userCache.Data as ClockifyUser;
                var projectRequest = projectCache == null || projectCache.Retrieved.Add(ValidDuration) < DateTimeOffset.Now
                    ? service?.Get($"v1/workspaces/{userData.Workspace}/projects")
                    : null;

                var start = now.Subtract(TimeSpan.FromDays(7)).ToString("O");
                var end = now.ToString("O");

                // The clockify API doesn't like the offset, so we need to replace with Z
                start = Regex.Replace(start, @"-..:..", "Z");
                end = Regex.Replace(end, @"-..:..", "Z");
                var timeEntryRequest = service?.Get($"v1/workspaces/{userData.Workspace}/user/{userData.Id}/time-entries?start={start}&end={end}");

                Task.WaitAll(new Task[] { projectRequest, timeEntryRequest });
                var projects = projectRequest != null
                    ? JsonConvert.DeserializeObject<List<Project>>((string)projectRequest.Result.Data)
                    : projectCache.Data as List<Project>;

                var timeEntries = JsonConvert.DeserializeObject<List<TimeEntry>>((string)timeEntryRequest.Result.Data);

                // Now that we have tags and time entries, we can build our response
                // We need to return a set of projects with their associated times in seconds
                Dictionary<string, int> timeTrackingResults = new Dictionary<string, int>();
                foreach(var entry in timeEntries)
                {
                    var projectName = projects
                        .Where(p => p.Id == entry.ProjectId)
                        .Select(p => p.Name)
                        .FirstOrDefault();

                    if(entry.TimeInterval.Duration != null)
                    {
                        var entryDuration = entry.TimeInterval?.Duration;
                        var hourMatch = Regex.Match(entryDuration, @"[0-9]+H");
                        var minuteMatch = Regex.Match(entryDuration, @"[0-9]+M");
                        var secondsMatch = Regex.Match(entryDuration, @"[0-9]+S");

                        int.TryParse(hourMatch.Value.TrimEnd('H'), out var hours);
                        int.TryParse(minuteMatch.Value.TrimEnd('M'), out var minutes);
                        int.TryParse(secondsMatch.Value.TrimEnd('S'), out var seconds);

                        var secondsDuration = seconds + (minutes * 60) + (hours * 60 * 60);

                        if (timeTrackingResults.TryGetValue(projectName, out var existingProjectDuration))
                        {
                            var newProjectDuration = existingProjectDuration + secondsDuration;
                            timeTrackingResults.AddOrSet(projectName, newProjectDuration);
                        }
                        else
                            timeTrackingResults.Add(projectName, secondsDuration);
                    }
                }

                Sender.Tell(timeTrackingResults);
            }
        }
    }
}

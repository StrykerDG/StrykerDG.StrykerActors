using Akka.Actor;
using Akka.Util.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using StrykerDG.StrykerActors.Twitch.Messages;
using StrykerDG.StrykerServices.Interfaces;
using StrykerDG.StrykerServices.TwitchService;
using StrykerDG.StrykerServices.TwitchService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace StrykerDG.StrykerActors.Twitch
{
    public class TwitchActor : StrykerActor
    {
        // Store the App Access Token required to interact with the Twitch API
        private string ClientId { get; set; }
        private string ClientSecret { get; set; }
        private AccessTokenRequest AccessToken { get; set; }
        private DateTimeOffset TokenRequestTime { get; set; }

        public TwitchActor(
            IServiceScopeFactory factory, 
            string clientId, 
            string clientSecret,
            TimeSpan duration
        ) : base(factory, duration)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;

            // First, we need to get an Access Token from twitch, and store that for later use
            GetTwitchApplicationToken();

            Receive<AskForTwitchUserProfile>(GetUserProfile);
        }

        public async Task RevokeToken()
        {
            using (var scope = ServiceScopeFactory.CreateScope()) 
            {
                var service = scope.ServiceProvider.GetServices<IStrykerService>()
                    .Where(s => s.GetType() == typeof(TwitchService))
                    .FirstOrDefault() as TwitchService;

                var endpoint = $"oauth2/revoke" +
                    $"?client_id={ClientId}" +
                    $"&token={AccessToken.AccessToken}";

                await service?.Post(endpoint);
            }
        }

        private void GetTwitchApplicationToken()
        {
            using (var scope = ServiceScopeFactory.CreateScope())
            {
                var service = scope.ServiceProvider.GetServices<IStrykerService>()
                    .Where(s => s.GetType() == typeof(TwitchService))
                    .FirstOrDefault() as TwitchService;


                AccessToken = service?.GetAppAccessToken(ClientId, ClientSecret);
            }
        }

        private async void GetUserProfile(AskForTwitchUserProfile message)
        {
            Cache.TryGetValue("USER_PROFILE", out var cacheResponse);

            if (
                cacheResponse == null ||
                cacheResponse?.Retrieved.Add(ValidDuration) < DateTimeOffset.Now
            )
            {
                using (var scope = ServiceScopeFactory.CreateScope())
                {
                    try
                    {
                        var service = scope.ServiceProvider.GetServices<IStrykerService>()
                            .Where(s => s.GetType() == typeof(TwitchService))
                            .FirstOrDefault() as TwitchService;

                        service.SetHttpClientHeaders(AccessToken.AccessToken, ClientId);

                        // Get the user information
                        var userResult = await service?.Get($"helix/users?login={message.Profile}");

                        // Result is in the form { "data": [ { ... }, { ... } ] }
                        var userResultObject = JsonConvert.DeserializeObject<JObject>((string)userResult.Data);
                        var userDataString = userResultObject.SelectToken("data").ToString();

                        // We want the first user
                        var userResultDataList = JsonConvert.DeserializeObject<List<TwitchUser>>(userDataString);
                        var user = userResultDataList.First();

                        // Get the follower information, now that we know the UserId
                        var followerResult = await service?.Get($"helix/users/follows?to_id={user.Id}");
                        var followers = JsonConvert.DeserializeObject<FollowsTo>((string)followerResult.Data);

                        var resultObject = new StrykerTwitchUser { User = user, Followers = followers };

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
                    catch (Exception ex)
                    {
                        Sender.Tell(ex);
                    }
                }
            }
            else
                Sender.Tell(cacheResponse.Data as StrykerTwitchUser);
        }
    }
}

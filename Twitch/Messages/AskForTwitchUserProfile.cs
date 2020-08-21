using System;
using System.Collections.Generic;
using System.Text;

namespace StrykerDG.StrykerActors.Twitch.Messages
{
    public class AskForTwitchUserProfile
    {
        public AskForTwitchUserProfile(string profile)
        {
            Profile = profile;
        }

        public string Profile { get; private set; }
    }
}

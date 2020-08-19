using System;
using System.Collections.Generic;
using System.Text;

namespace StrykerDG.StrykerActors.GitHub.Messages
{
    public class AskForUserProfile
    {
        public AskForUserProfile(string profile)
        {
            Profile = profile;
        }

        public string Profile { get; private set; }
    }
}

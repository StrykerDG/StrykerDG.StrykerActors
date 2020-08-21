using System;
using System.Collections.Generic;
using System.Text;

namespace StrykerDG.StrykerActors.GitHub.Messages
{
    public class AskForGitHubUserProfile
    {
        public AskForGitHubUserProfile(string profile)
        {
            Profile = profile;
        }

        public string Profile { get; private set; }
    }
}

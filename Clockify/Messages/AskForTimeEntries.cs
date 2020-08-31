using System;
using System.Collections.Generic;
using System.Text;

namespace StrykerDG.StrykerActors.Clockify.Messages
{
    public class AskForTimeEntries
    {
        public AskForTimeEntries(
            string workspace, 
            string user, 
            Dictionary<string, dynamic> filters = null
        )
        {
            WorkSpace = workspace;
            User = user;
            Filters = filters;
        }

        public string WorkSpace { get; private set; }
        public string User { get; private set; }

        // See https://clockify.me/developers-api#tag-Time-entry for appropriate filters
        public Dictionary<string, dynamic> Filters { get; private set; }
    }
}

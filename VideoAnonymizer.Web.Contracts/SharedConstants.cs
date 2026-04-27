using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Web.Shared
{
    public class SharedConstants
    {
        public static class Paths
        {
            public const string Analyzed = "analyzed";
            public const string Analyze = "analyze";
            public const string Anonymized = "anonymized";
            public const string Anonymize = "anonymize";
            public const string Health = "health";
            public const string Video = "video";

            public const string AppState = "state";
        }

        public static class SignalR
        {
            public const string JobHubUrl = "/hubs/jobs";
            public static class Messages
            {
                public const string VideoAnalyzed = "videoAnalyzed";
                public const string VideoAnonymized = "videoAnonymized";
                public const string JobProgress = "jobProgress";

            }
            public static class Status
            {
                public const string Completed = "completed";
            }
        }
    }
}

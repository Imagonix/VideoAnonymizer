using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Contracts.RabbitMQ
{
    public class RabbitMQConstants
    {
        public class Queues
        {
            public const string Analyze= RoutingKeys.Analyze + ".queue";
            public const string Analyzed = RoutingKeys.Analyzed + ".queue";
            public const string Anonymize = RoutingKeys.Anonymize + ".queue";
            public const string Anonymized = RoutingKeys.Anonymized + ".queue";
            public const string Progress = RoutingKeys.Progress + ".queue";
            public const string TrackForward = RoutingKeys.TrackForward + ".queue";
            public const string TrackForwardCompleted = RoutingKeys.TrackForwardCompleted + ".queue";
        }
        public class RoutingKeys
        {
            public const string Analyze = "video.analyze";
            public const string Analyzed = "video.analyzed";
            public const string Anonymize = "video.anonymize";
            public const string Anonymized = "video.anonymized";
            public const string Progress = "video.progress";
            public const string TrackForward = "video.track-forward";
            public const string TrackForwardCompleted = "video.track-forward-completed";
        }
    }
}

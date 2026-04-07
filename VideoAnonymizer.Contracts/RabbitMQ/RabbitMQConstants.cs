using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Contracts.RabbitMQ
{
    public class RabbitMQConstants
    {
        public class Queues
        {
            public const string VideoProcessing = "video.processing";
            public const string VideoNotifications = "video.notifications";
        }
        public class RoutingKeys
        {
            public const string Analyze = "video.analyze";
            public const string Analyzed = "video.analyzed";
            public const string Anonymize = "video.anonymize";
            public const string Anonymized = "video.anonymized";
        }
    }
}

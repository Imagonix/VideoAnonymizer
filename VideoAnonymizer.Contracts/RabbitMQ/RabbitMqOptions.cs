using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Contracts.RabbitMQ;

public sealed class RabbitMqOptions
{
    public string ConnectionString { get; set; } = default!;
    public string ExchangeName { get; set; } = "video-anonymizer";
}
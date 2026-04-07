using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Contracts.RabbitMQ;

public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
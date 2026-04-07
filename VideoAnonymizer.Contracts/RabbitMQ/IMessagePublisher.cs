using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Contracts.RabbitMQ;

public interface IMessagePublisher
{
    Task PublishAsync<T>(
        string routingKey,
        T message,
        CancellationToken cancellationToken = default);
}
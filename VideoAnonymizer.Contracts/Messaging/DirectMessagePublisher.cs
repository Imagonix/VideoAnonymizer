using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VideoAnonymizer.Contracts.Messaging;

public sealed class DirectMessagePublisher(
    IServiceScopeFactory scopeFactory,
    ILogger<DirectMessagePublisher> logger) : IMessagePublisher
{
    public async Task PublishAsync<T>(
        string routingKey,
        T message,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider
            .GetServices<IMessageHandler<T>>()
            .ToList();

        if (handlers.Count == 0)
        {
            logger.LogWarning(
                "No direct message handler registered for {MessageType} on route {RoutingKey}",
                typeof(T).Name,
                routingKey);
            return;
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(message, cancellationToken);
        }
    }
}

namespace VideoAnonymizer.Contracts.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(
        string routingKey,
        T message,
        CancellationToken cancellationToken = default);
}

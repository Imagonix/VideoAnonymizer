using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using VideoAnonymizer.Contracts.Messaging;

namespace VideoAnonymizer.Contracts.RabbitMQ;

public sealed class RabbitMqMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqMessagePublisher(
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null && _channel is not null)
            return;

        _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);
    }

    public async Task PublishAsync<T>(
        string routingKey,
        T message,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = typeof(T).Name
        };

        await _channel!.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}

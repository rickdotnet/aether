using Aether.Abstractions.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;

namespace Aether.Providers.NATS.Messaging;

/// <summary>
/// Provides a factory for creating aether components that use NATS as the underlying messaging system.
/// </summary>
/// <remarks>This factory does not manage the lifecycle of the providers it creates.</remarks>
public sealed class aetherFactory
{
    private readonly INatsConnection connection;
    private readonly ILoggerFactory loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="aetherFactory"/> class.
    /// </summary>
    /// <param name="natsConnection">Underlying NATS connection</param>
    /// <param name="loggerFactory">(Optional) Logger Factory</param>
    public aetherFactory(INatsConnection natsConnection, ILoggerFactory? loggerFactory)
    {
        connection = natsConnection;
        this.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    public ISubscriptionProvider CreateSubscriptionProvider()
        => new NatsSubscriptionProvider(connection, loggerFactory);

    public IPublisherProvider CreatePublisher()
        => new NatsPublisher(connection);
}
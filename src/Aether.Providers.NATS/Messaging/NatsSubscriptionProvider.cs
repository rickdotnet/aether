using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Abstractions.Providers;
using Aether.Messaging;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using RickDotNet.Base;

namespace Aether.Providers.NATS.Messaging;

public class NatsSubscriptionProvider : ISubscriptionProvider
{
    private readonly INatsConnection connection;
    private readonly ILoggerFactory loggerFactory;

    public NatsSubscriptionProvider(INatsConnection connection, ILoggerFactory loggerFactory)
    {
        this.connection = connection;
        this.loggerFactory = loggerFactory;
    }

    public ISubscription AddSubscription(SubscriptionConfig subConfig,
        Func<MessageContext, CancellationToken, Task<Result<VoidResult>>> handler)
    {
        var subscriptionContext = new SubscriptionContext(subConfig, handler);
        return subscriptionContext.IsJetStream
            ? new NatsJetStreamSubscription(
                connection,
                loggerFactory.CreateLogger<NatsJetStreamSubscription>(),
                subscriptionContext)
            : new NatsCoreSubscription(
                connection,
                loggerFactory.CreateLogger<NatsCoreSubscription>(),
                subscriptionContext);
    }
}

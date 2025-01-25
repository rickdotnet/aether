using Aether.Abstractions.Messaging;
using Aether.Abstractions.Providers;
using Aether.Messaging;
using Aether.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

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

    public ISubscription AddSubscription(SubscriptionConfig config,
        Func<MessageContext, CancellationToken, Task> handler)
    {
        
        // return new NatsCoreSubscription(
        //     connection,
        //     loggerFactory.CreateLogger<NatsCoreSubscription>(),
        //     config,
        //     handler);
        
        
        if (config.ConsumerConfig.DurableType == DurableType.Durable)
        {
           return new NatsJetStreamSubscription(
                   connection,
                   loggerFactory.CreateLogger<NatsJetStreamSubscription>(),
                   config,
                   handler);
        }
        else
        {
            return new NatsCoreSubscription(
                connection,
                loggerFactory.CreateLogger<NatsCoreSubscription>(),
                config,
                handler);
        }
    }
}
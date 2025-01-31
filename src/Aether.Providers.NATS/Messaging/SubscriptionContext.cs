using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;
using NATS.Client.JetStream.Models;
using RickDotNet.Base;

namespace Aether.Providers.NATS.Messaging;

public class SubscriptionContext
{
    private const string ConsumerConfigKey = "nats-consumer-config";
    
    public Func<MessageContext, CancellationToken, Task<Result<VoidResult>>> Handler;
    public SubscriptionConfig SubscriptionConfig { get; }
    public ConsumerConfig? ConsumerConfig { get; }

    public bool IsJetStream => ConsumerConfig != null;
    
    public SubscriptionContext(SubscriptionConfig subConfig, Func<MessageContext, CancellationToken, Task<Result<VoidResult>>> handler)
    {
        SubscriptionConfig = subConfig;
        Handler = handler;
        ConsumerConfig = subConfig.EndpointConfig.ProviderConfig.GetValueOrDefault(ConsumerConfigKey) as ConsumerConfig;
    }

}
using Aether.Abstractions.Messaging.Configuration;
using ConsumerConfig = NATS.Client.JetStream.Models.ConsumerConfig;

namespace Aether.Providers.NATS.Messaging;

public static class EndpointConfigExtensions
{
    public static EndpointConfig WithConsumer(this EndpointConfig config, ConsumerConfig consumerConfig)
    {
        var copy = config with { };
        copy.ProviderConfig["nats-consumer-config"] = consumerConfig;
        
        return copy;
    }
}

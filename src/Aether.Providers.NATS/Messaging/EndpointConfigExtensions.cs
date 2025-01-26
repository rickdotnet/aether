using Aether.Abstractions.Messaging.Configuration;
using ConsumerConfig = NATS.Client.JetStream.Models.ConsumerConfig;

namespace Aether.Providers.NATS.Messaging;

public static class EndpointConfigExtensions
{
    public static void SetConsumerConfig(this EndpointConfig config, ConsumerConfig consumerConfig)
    {
        config.ProviderConfig["nats-consumer-config"] = consumerConfig;
    }
}

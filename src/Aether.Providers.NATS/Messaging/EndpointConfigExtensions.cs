using Aether.Abstractions.Messaging;
using NATS.Client.JetStream.Models;

namespace Aether.Providers.NATS.Messaging;

public static class EndpointConfigExtensions
{
    public static EndpointConfig WithConsumer(this EndpointConfig config, ConsumerConfig consumerConfig)
    {
        var copy = consumerConfig with { };
        config.HubConfig["nats-consumer-config"] = copy;
        
        return config;
    }
}

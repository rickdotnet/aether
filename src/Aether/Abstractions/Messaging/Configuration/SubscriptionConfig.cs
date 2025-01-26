using Aether.Messaging;

namespace Aether.Abstractions.Messaging.Configuration;

public record SubscriptionConfig
{
    public required EndpointConfig EndpointConfig { get; init; }
    public required Type[] MessageTypes { get; init; } = [];
    public Type? EndpointType { get; set; }

    public bool HandlerOnly => EndpointType == null;

    public static SubscriptionConfig ForEndpoint(EndpointConfig endpointConfig, Type? endpointType = null, Type[]? messageTypes = null)
        => new()
        {
            EndpointConfig = endpointConfig,
            EndpointType = endpointType,
            MessageTypes = messageTypes ?? endpointType?.GetHandlerTypes() ?? [],
        };
}

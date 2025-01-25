namespace Aether.Messaging.Configuration;

public record SubscriptionConfig
{
    public required ConsumerConfig ConsumerConfig { get; set; }
    public string? Namespace { get; set; }
    public string? EndpointName { get; init; }
    public string? Subject { get; set; }
    public Type? EndpointType { get; set; }
    public required Type[] MessageTypes { get; init; } = [];

    public static SubscriptionConfig ForEndpoint(EndpointConfig endpointConfig, Type? endpointType = null, Type[]? messageTypes = null)
        => new()
        {
            ConsumerConfig = endpointConfig.ConsumerConfig,
            Namespace = endpointConfig.Namespace,
            EndpointName = endpointConfig.EndpointName,
            Subject = endpointConfig.Subject,
            EndpointType = endpointType,
            MessageTypes = messageTypes ?? endpointType?.GetHandlerTypes() ?? [],
        };
}

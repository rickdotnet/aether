using Aether.Abstractions.Messaging;
using Aether.Abstractions.Providers;

namespace Aether.Messaging.Configuration;

public record ConsumerConfig
{
    public static ConsumerConfig Default => new();

    /// <summary>
    /// Unique name for the consumer 
    /// </summary>
    public string Name { get; set; } = $"aether-{Guid.NewGuid().ToString()[..4]}";
    
    /// <summary>
    /// Indicates the type of consumer to use
    /// </summary>
    public DurableType DurableType { get; set; } = DurableType.Transient;
    
    // need to configure the NATS consumer options
    // max ack pending, etc
    
    public AckStrategy AckStrategy { get; set; } = AckStrategy.Default;
}

public enum DurableType
{
    Transient,
    //Ephemeral,
    Durable,
}
public record EndpointConfig
{
    /// <summary>
    /// Defaults to AetherConfig.InstanceId
    /// </summary>
    public string? InstanceId { get; internal set; }
    
    /// <summary>
    /// Optional namespace for isolation. Defaults to AetherConfig.DefaultNamespace
    /// </summary>
    public string? Namespace { get; set; }
    
    /// <summary>
    /// Display name. Used to create a unique endpoint name if no subject is provided
    /// </summary>
    public string? EndpointName { get; init; }
    
    /// <summary>
    /// The subject to use for the endpoint. If not provided, the endpoint name will be slugified. If neither are provided, the namespace will be used.
    /// </summary>
    public string? Subject { get; set; } // endpoint name or subject must be provided

    /// <summary>
    /// Optional consumer configuration. Default is transient consumer with InstanceId as the consumer name
    /// </summary>
    public ConsumerConfig ConsumerConfig { get; set; } = ConsumerConfig.Default;
    

    /// <summary>
    /// Set by AetherClient
    /// </summary>
    internal Type? EndpointType { get; set; }
}

public static class EndpointConfigExtensions
{
    public static PublishConfig ToPublishConfig(this EndpointConfig config)
    {
        return new PublishConfig
        {
            Namespace = config.Namespace,
            EndpointName = config.EndpointName,
            Subject = config.Subject
        };
    }
}
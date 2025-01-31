namespace Aether.Abstractions.Messaging.Configuration;

public sealed record EndpointConfig
{
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
    /// This is a temporary solution to provider specific configuration
    /// </summary>
    public Dictionary<string, object> ProviderConfig  { get; } = new();
}

public static class EndpointConfigExtensions
{
    public static PublishConfig ToPublishConfig(this EndpointConfig config)
    {
        return new PublishConfig
        {
            Namespace = config.Namespace,
            EndpointName = config.EndpointName,
            Subject = config.Subject,
        };
    }
}
namespace Aether.Abstractions.Messaging;

public sealed record EndpointConfig
{
    /// <summary>
    /// Optional namespace for isolation.
    /// </summary>
    public string? Namespace { get; set; }
    
    /// <summary>
    /// Display name
    /// </summary>
    public string? EndpointName { get; init; }
    
    /// <summary>
    /// The subject to use for the endpoint.
    /// </summary>
    public string Subject { get; set; }
    
    // temporary solution until we decide what we want to do
    public string FullSubject => Namespace != null ? $"{Namespace}.{Subject}" : Subject;
    
    /// <summary>
    /// This is a temporary solution to provider specific configuration
    /// </summary>
    public Dictionary<string, object> ProviderConfig  { get; } = new();
    
    public EndpointConfig(string subject)
    {
        Subject = subject;
    }

    public EndpointConfig()
    {
    }
}

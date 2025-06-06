namespace Aether.Abstractions.Messaging;

public sealed record EndpointConfig
{
    /// <summary>
    /// The subject to use for the endpoint.
    /// </summary>
    public string Subject { get; init; }

    /// <summary>
    /// Optional unique identifier for the endpoint.
    /// </summary>
    /// <remarks>If not provided, a unique identifier will be generated.</remarks>
    public string? EndpointId { get; init; }
    
    /// <summary>
    /// Optional namespace for isolation.
    /// </summary>
    public string? Namespace { get; init; }

    public string SubjectDelimiter { get; init; } = ".";

    // temporary solution until we decide what we want to do
    public string FullSubject => Namespace != null ? $"{Namespace}{SubjectDelimiter}{Subject}" : Subject;


    /// <summary>
    /// This is a temporary solution to hub specific configuration; will be revisited.
    /// </summary>
    public Dictionary<string, object> HubConfig { get; } = new();

    public EndpointConfig(string subject, string? @namespace = null, string subjectDelimiter = ".")
    {
        Namespace = @namespace;
        Subject = subject;
        SubjectDelimiter = subjectDelimiter;
    }

    public static EndpointConfig For(string subject)
        => new(subject);

    public static EndpointConfig For(string @namespace, string subject)
        => new(subject, @namespace);
}

public static class EndpointConfigExtensions
{
       public static EndpointConfig WithNamespace(this EndpointConfig config, string @namespace)
        => config with { Namespace = @namespace };

    public static EndpointConfig WithSubjectDelimiter(this EndpointConfig config, string delimiter)
        => config with { SubjectDelimiter = delimiter };

    public static EndpointConfig WithHubConfig(this EndpointConfig config, string key, object value)
    {
        config.HubConfig.Add(key, value);
        return config;
    }
}

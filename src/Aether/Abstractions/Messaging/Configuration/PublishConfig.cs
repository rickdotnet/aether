namespace Aether.Abstractions.Messaging.Configuration;

public record PublishConfig
{
    public string? Namespace { get; set; } // namespace will prefix endpoint name
    public string? EndpointName { get; init; } // endpoint name or subject must be provided
    public string? Subject { get; set; } // endpoint name or subject must be provided
}
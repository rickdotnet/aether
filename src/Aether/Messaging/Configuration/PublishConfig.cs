using Aether.Abstractions.Providers;

namespace Aether.Messaging.Configuration;

public record PublishConfig
{
    public string? Namespace { get; set; } // namespace will prefix endpoint name
    public string? EndpointName { get; init; } // endpoint name or subject must be provided
    public string? Subject { get; set; } // endpoint name or subject must be provided
    public IPublisherProvider? PublisherProvider { get; set; } // optionally override the DI providerPublisher
}
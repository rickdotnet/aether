using Aether.Abstractions.Messaging.Configuration;

namespace Aether.Abstractions.Messaging;

public interface IPublisherProvider
{
    Task Publish(PublishConfig publishConfig, AetherMessage message, CancellationToken cancellationToken);
    
    Task<AetherData> Request(PublishConfig publishConfig, AetherMessage message, CancellationToken cancellationToken);
}
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;

namespace Aether.Abstractions.Providers;

public interface IPublisherProvider
{
    Task Publish(PublishConfig publishConfig, AetherMessage message, CancellationToken cancellationToken);
    
    Task<byte[]> Request(PublishConfig publishConfig, AetherMessage message, CancellationToken cancellationToken);
}
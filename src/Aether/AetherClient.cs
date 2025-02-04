using Aether.Abstractions.Messaging;
using Aether.Abstractions.Storage;
using Aether.Messaging;
using Aether.Providers.Memory;
using Aether.Providers.Storage;

namespace Aether;

public interface IAetherClient
{
    IDefaultMessageHub Messaging { get; }
    IDefaultStorageProvider Storage { get; }
}

public class AetherClient : IAetherClient
{
    public static readonly AetherClient MemoryClient = CreateMemoryClient();
    public IDefaultMessageHub Messaging { get; }
    public IDefaultStorageProvider Storage { get; }

    public AetherClient(IDefaultMessageHub messaging, IDefaultStorageProvider storage)
    {
        Messaging = messaging;
        Storage = storage;
    }

    private static AetherClient CreateMemoryClient()
    {
        var inMemoryProvider = new InMemoryHubProvider();
        var storageProvider = new InMemoryStorageProvider();

        var syncHub = new ChannelBackedHub(
            subProvider: inMemoryProvider,
            publisherProvider: inMemoryProvider
        );

        return new AetherClient(
            new DefaultMessageHub(syncHub),
            new DefaultStorageProvider(storageProvider)
        );
    }

    public static AetherClient CreateClient(ISubscriptionProvider subscriptionProvider, IPublisherProvider publisherProvider)
    {
        var syncHub = new ChannelBackedHub(subscriptionProvider, publisherProvider);

        return new AetherClient(
            new DefaultMessageHub(syncHub),
            new DefaultStorageProvider(new InMemoryStorageProvider())
        );
    }
}

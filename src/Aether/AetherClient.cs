using Aether.Abstractions.Providers;
using Aether.Abstractions.Storage;
using Aether.Messaging;
using Aether.Providers.Memory;
using Aether.Storage;

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
        var inMemoryProvider = new InMemoryMessageHubProvider();
        var storageProvider = new InMemoryStorageProvider();

        var syncHub = new SynchronousHub(
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
        var syncHub = new SynchronousHub(subscriptionProvider, publisherProvider);

        return new AetherClient(
            new DefaultMessageHub(syncHub),
            new DefaultStorageProvider(new InMemoryStorageProvider())
        );
    }
}

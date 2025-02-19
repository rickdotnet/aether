using Aether.Abstractions.Messaging;
using Aether.Abstractions.Storage;
using Aether.Messaging;
using Aether.Providers.Memory;

namespace Aether;

public interface IAetherClient
{
    IDefaultMessageHub Messaging { get; }
    IDefaultStore Storage { get; }
}

public class AetherClient : IAetherClient
{
    public static readonly AetherClient MemoryClient = CreateMemoryClient();
    public IDefaultMessageHub Messaging { get; }
    public IDefaultStore Storage { get; }

    public AetherClient(IDefaultMessageHub messaging, IDefaultStore storage)
    {
        Messaging = messaging;
        Storage = storage;
    }

    public static AetherClient CreateMemoryClient(IEndpointProvider? endpointProvider = null)
    {
        var inMemoryProvider = new MemoryHubProvider();
        var inMemoryStore = new MemoryStore();

        var syncHub = new ChannelBackedHub(
            subProvider: inMemoryProvider,
            publisherProvider: inMemoryProvider,
            endpointProvider: endpointProvider
        );

        return new AetherClient(
            new DefaultMessageHub(syncHub),
            new DefaultStore(inMemoryStore)
        );
    }

    public static AetherClient CreateClient(ISubscriptionProvider subscriptionProvider, IPublisherProvider publisherProvider)
    {
        var syncHub = new ChannelBackedHub(subscriptionProvider, publisherProvider);

        return new AetherClient(
            new DefaultMessageHub(syncHub),
            new DefaultStore(new MemoryStore())
        );
    }
}

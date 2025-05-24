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

    public AetherClient(IMessageHub messaging, IStore storage)
    {
        Messaging = new DefaultMessageHub(messaging);
        Storage = new DefaultStore(storage);
    }

    private static AetherClient CreateMemoryClient()
    {
        var memoryHub = new MemoryHub();
        var memoryStore = new MemoryStore();

        return new AetherClient(
            new DefaultMessageHub(memoryHub),
            new DefaultStore(memoryStore)
        );
    }
}

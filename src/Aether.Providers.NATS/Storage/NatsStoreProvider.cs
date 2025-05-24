using Aether.Abstractions.Hosting;
using Aether.Abstractions.Storage;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;
using NATS.Net;

namespace Aether.Providers.NATS.Storage;

public sealed class NatsStoreProvider //: IStoreProvider
{
    private readonly INatsKVContext kvContext;

    public NatsStoreProvider(INatsConnection nats)
    {
        kvContext = nats.CreateKeyValueStoreContext();
    }

    public async Task<IStore> CreateStore(StorageRegistration registration,
        CancellationToken cancellationToken = default)
    {
        return new NatsKvStore(
            await kvContext.CreateStoreAsync(
                new NatsKVConfig(registration.StoreName)
                {
                    MaxBytes = registration.MaxBytes ?? 0,
                },
                cancellationToken)
        );
    }
}

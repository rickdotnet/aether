using Aether.Abstractions.Hosting;
using Aether.Abstractions.Storage;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;
using NATS.Net;

namespace Aether.Providers.NATS.Storage;

public sealed class NatsStorageProviderFactory : IStorageProviderFactory
{
    private readonly INatsKVContext kvContext;

    public NatsStorageProviderFactory(INatsConnection nats)
    {
        kvContext = nats.CreateKeyValueStoreContext();
    }

    public async Task<IStorageProvider> CreateStore(StorageRegistration registration,
        CancellationToken cancellationToken = default)
    {
        return new NatsKvStorageProvider(
            await kvContext.CreateStoreAsync(registration.StoreName, cancellationToken)
        );
    }
}
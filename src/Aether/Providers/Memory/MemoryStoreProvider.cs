using Aether.Abstractions.Hosting;
using Aether.Abstractions.Storage;

namespace Aether.Providers.Memory;

public class MemoryStoreProvider : IStoreProvider
{
    public Task<IStore> CreateStore(StorageRegistration registration, CancellationToken cancellationToken = default)
        => Task.FromResult<IStore>(new MemoryStore());
}

public static class StorageBuilderExtensions
{
    public static IStorageBuilder AddMemoryStore(this IStorageBuilder storageBuilder, string storeName)
    {
        storageBuilder.AddStore<MemoryStoreProvider>(storeName);
        return storageBuilder;
    }
}

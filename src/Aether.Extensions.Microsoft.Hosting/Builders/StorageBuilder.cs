using Aether.Abstractions.Hosting;
using Aether.Abstractions.Storage;
using Aether.Providers.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Extensions.Microsoft.Hosting.Builders;

public class StorageBuilder : IStorageBuilder
{
    public Type DefaultStoreType { get; private set; } = typeof(MemoryStore);
    private readonly AetherBuilder aetherBuilder;

    private HashSet<string> storeNames = new();

    public StorageBuilder(AetherBuilder aetherBuilder)
    {
        this.aetherBuilder = aetherBuilder;
    }

    public IStorageBuilder AddStore<T>() where T : IStore
        => AddStore<T>(IDefaultStore.DefaultStoreName);
    
    public IStorageBuilder AddStore<T>(int maxBytes) where T : IStore
        => AddStore<T>(IDefaultStore.DefaultStoreName, maxBytes);

    public IStorageBuilder AddStore<T>(string storeName, int maxBytes = 0) where T : IStore
        => AddStore(StorageRegistration.From<T>(storeName));

    private IStorageBuilder AddStore(StorageRegistration registration)
    {
        if (!storeNames.Add(registration.StoreName))
            throw new InvalidOperationException($"Store with name {registration.StoreName} already exists.");

        if (registration.StoreName == IDefaultStore.DefaultStoreName)
            DefaultStoreType = registration.StoreType!;
        
        aetherBuilder.RegisterServices(services =>
        {
            services.AddSingleton(registration.StoreType!);
            services.AddSingleton(registration);
        });
        return this;
    }
}

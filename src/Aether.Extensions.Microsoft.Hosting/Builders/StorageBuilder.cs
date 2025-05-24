using Aether.Abstractions.Hosting;
using Aether.Abstractions.Storage;
using Aether.Providers.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aether.Extensions.Microsoft.Hosting.Builders;

public class StorageBuilder : IStorageBuilder
{
    public Type DefaultStoreType { get; private set; } = typeof(MemoryStore);
    
    public List<StoreRegistration> StoreRegistrations { get; } = [];
    private readonly AetherBuilder aetherBuilder;

    private readonly HashSet<string> storeNames = [];

    public StorageBuilder(AetherBuilder aetherBuilder)
    {
        this.aetherBuilder = aetherBuilder;
    }

    public IStorageBuilder AddStore<T>() where T : IStore
        => AddStore<T>(IDefaultStore.DefaultStoreName);
    
    public IStorageBuilder AddStore<T>(int maxBytes) where T : IStore
        => AddStore<T>(IDefaultStore.DefaultStoreName, maxBytes);

    public IStorageBuilder AddStore<T>(string storeName, int maxBytes = 0) where T : IStore
        => AddStore(StoreRegistration.From<T>(storeName));

    private IStorageBuilder AddStore(StoreRegistration registration)
    {
        if (!storeNames.Add(registration.StoreName))
            throw new InvalidOperationException($"Store with name {registration.StoreName} already exists.");

        if (registration.StoreName == IDefaultStore.DefaultStoreName)
            DefaultStoreType = registration.StoreType!;
        
        StoreRegistrations.Add(registration);
        
        return this;
    }
    
    public void RegisterServices(Action<IServiceCollection> configureServices)
    {
        aetherBuilder.RegisterServices(configureServices);
    }
}

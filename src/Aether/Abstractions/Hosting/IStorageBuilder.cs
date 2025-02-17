using Aether.Abstractions.Storage;

namespace Aether.Abstractions.Hosting;

public interface IStorageBuilder
{
    public IStorageBuilder AddStore<T>() where T : IStoreProvider;

    public IStorageBuilder AddStore<T>(string storeName) where T : IStoreProvider;

    public IStorageBuilder AddStore(StorageRegistration registration);
}
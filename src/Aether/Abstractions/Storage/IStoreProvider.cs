using Aether.Abstractions.Hosting;

namespace Aether.Abstractions.Storage;

public interface IStoreProvider
{
    Task<IStore> CreateStore(
        StorageRegistration registration,
        CancellationToken cancellationToken = default);
}
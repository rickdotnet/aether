using Aether.Abstractions.Hosting;

namespace Aether.Abstractions.Storage;

public interface IStorageProviderFactory
{
    Task<IStorageProvider> CreateStore(
        StorageRegistration registration,
        CancellationToken cancellationToken = default);
}
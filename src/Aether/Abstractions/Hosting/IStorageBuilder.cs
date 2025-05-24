using Aether.Abstractions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Abstractions.Hosting;

public interface IStorageBuilder
{
    public Type DefaultStoreType { get; }
    public IStorageBuilder AddStore<T>() where T : IStore;
    public IStorageBuilder AddStore<T>(string storeName, int maxBytes = 0) where T : IStore;
    
    public void RegisterServices(Action<IServiceCollection> configureServices);
}
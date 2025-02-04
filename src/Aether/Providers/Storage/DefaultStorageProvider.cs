using Aether.Abstractions.Storage;
using RickDotNet.Base;

namespace Aether.Providers.Storage;

public class DefaultStorageProvider :  IDefaultStorageProvider
{
    private readonly Dictionary<string, IStorageProvider> stores;
    private IStorageProvider DefaultStore => stores[IDefaultStorageProvider.DefaultStoreName];

    // for now, we're just going to wrap a single provider
    // will add the ability to 'add' or 'choose' providers later
    public DefaultStorageProvider(IStorageProvider defaultStore)
    {
        stores = new Dictionary<string, IStorageProvider>
        {
            [IDefaultStorageProvider.DefaultStoreName] = defaultStore,
        };
    }

    public IStorageProvider AsProvider() => DefaultStore;
    
    public Result<IStorageProvider> GetStore(string storeName)
    {
        // result or failure
        return Result.Try(() => stores[storeName]);
    }

    public Result<VoidResult> SetStore(string storeName, IStorageProvider store)
    {
        return Result.Try(() =>
        {
            stores[storeName] = store;    
        });
    }

    public ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default) 
        => DefaultStore.Get(id, token);

    public ValueTask<Result<AetherData>> Insert(string id, AetherData data, CancellationToken token = default) 
        => DefaultStore.Insert(id, data, token);

    public ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default) 
        => DefaultStore.Delete(id, token);

    public ValueTask<Result<IEnumerable<string>>> ListKeys(CancellationToken token = default) 
        => DefaultStore.ListKeys(token);

    public ValueTask<Result<IEnumerable<AetherData>>> List<TData>(FilterCriteria<TData>? filterCriteria = null, CancellationToken token = default) 
        => DefaultStore.List(filterCriteria, token);
}

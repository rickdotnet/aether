using RickDotNet.Base;

namespace Aether.Abstractions.Storage;

public class DefaultStore :  IDefaultStore
{
    private readonly Dictionary<string, IStore> stores;
    private IStore Default => stores[IDefaultStore.DefaultStoreName];

    public DefaultStore(IStore defaultStore)
    {
        stores = new Dictionary<string, IStore>
        {
            [IDefaultStore.DefaultStoreName] = defaultStore,
        };
    }

    public IStore AsStore() => Default;
    
    public Result<IStore> GetStore(string storeName) 
        => Result.Try(() => stores[storeName]);

    public Result<VoidResult> SetStore(string storeName, IStore store) 
        => Result.Try(() =>
        {
            stores[storeName] = store;    
        });

    public ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default) 
        => Default.Get(id, token);

    public ValueTask<Result<AetherData>> Insert(string id, AetherData data, CancellationToken token = default) 
        => Default.Insert(id, data, token);

    public ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default) 
        => Default.Delete(id, token);

    public ValueTask<Result<IEnumerable<string>>> ListKeys(CancellationToken token = default) 
        => Default.ListKeys(token);

    public ValueTask<Result<IEnumerable<AetherData>>> List<TData>(FilterCriteria<TData>? filterCriteria = null, CancellationToken token = default) 
        => Default.List(filterCriteria, token);
}

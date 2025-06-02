using RickDotNet.Base;
using RickDotNet.Extensions.Base;

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

    public Result<Unit> SetStore(string storeName, IStore store) 
        => Result.Try(() =>
        {
            stores[storeName] = store;    
        });

    public ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default) 
        => Default.Get(id, token);

    public async ValueTask<Result<T>> Get<T>(string id, CancellationToken token)
    {
        var storeResult = await Get(id, token);
        var valueResult = storeResult.Select(d => d.As<T>() ?? default);
        
        return valueResult.ValueOrDefault() == null 
            ? Result.Error<T>("No value, buddy.") 
            : valueResult!;
    }

    public ValueTask<Result<AetherData>> Insert(string id, AetherData data, CancellationToken token = default) 
        => Default.Insert(id, data, token);

    public async ValueTask<Result<T>> Insert<T>(string id, T data, CancellationToken token = default)
    {
        var result = await Insert(id, AetherData.Serialize(data), token);
        return result.Select(d => d.As<T>() ?? data);
    }

    public ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default) 
        => Default.Delete(id, token);

    public ValueTask<Result<IEnumerable<string>>> ListKeys(CancellationToken token = default) 
        => Default.ListKeys(token);

    public ValueTask<Result<IEnumerable<AetherData>>> List<TData>(FilterCriteria<TData>? filterCriteria = null, CancellationToken token = default) 
        => Default.List(filterCriteria, token);
}

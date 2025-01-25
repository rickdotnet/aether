using Aether.Abstractions.Storage;
using RickDotNet.Base;

namespace Aether.Storage;

public class DefaultStorageProvider :  IDefaultStorageProvider
{
    private readonly IStorageProvider defaultProvider;

    // for now, we're just going to wrap a single provider
    // will add the ability to 'add' or 'choose' providers later
    public DefaultStorageProvider(IStorageProvider defaultProvider)
    {
        this.defaultProvider = defaultProvider;
    }

    public IStorageProvider AsProvider() => defaultProvider;

    public IStorageProvider GetProvider(string providerKey) => throw new NotImplementedException();

    public TProvider GetProvider<TProvider>() where TProvider : IStorageProvider => throw new NotImplementedException();
    
    public ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default) => throw new NotImplementedException();

    public ValueTask<Result<AetherData>> Insert(string id, AetherData data, CancellationToken token = default) => throw new NotImplementedException();

    public ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default) => throw new NotImplementedException();

    public ValueTask<Result<IEnumerable<string>>> ListKeys(FilterCriteria<string>? filterCriteria = null, CancellationToken token = default) => throw new NotImplementedException();

    public ValueTask<Result<IEnumerable<AetherData>>> List<TData>(FilterCriteria<TData>? filterCriteria = null, CancellationToken token = default) => throw new NotImplementedException();


}

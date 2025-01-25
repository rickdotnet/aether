using Aether.Abstractions.Storage;
using Aether.Storage;
using RickDotNet.Base;

namespace Aether.Providers.Memory;

public class InMemoryStorageProvider : IStorageProvider
{
    public ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result<AetherData>> Insert(string id, AetherData data, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result<IEnumerable<string>>> ListKeys(FilterCriteria<string>? filterCriteria = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result<IEnumerable<AetherData>>> List<TData>(FilterCriteria<TData>? filterCriteria = null, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}

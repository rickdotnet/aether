using RickDotNet.Base;

namespace Aether.Abstractions.Storage;

public interface IStore
{
    ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default);
    ValueTask<Result<AetherData>> Insert(string id, AetherData data, CancellationToken token = default);
    ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default);
    ValueTask<Result<IEnumerable<string>>> ListKeys(CancellationToken token = default);
    ValueTask<Result<IEnumerable<AetherData>>> List<TData>(FilterCriteria<TData>? filterCriteria = null, CancellationToken token = default);
}

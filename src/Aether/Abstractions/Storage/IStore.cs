using RickDotNet.Base;

namespace Aether.Abstractions.Storage;

public interface IStore
{
    ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default);
    ValueTask<Result<T>> Get<T>(string id, CancellationToken token = default);
    ValueTask<Result<AetherData>> Upsert(string id, AetherData data, CancellationToken token = default);
    ValueTask<Result<T>> Upsert<T>(string id, T data, CancellationToken token = default);
    ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default);
    ValueTask<Result<IEnumerable<string>>> ListKeys(CancellationToken token = default);
}

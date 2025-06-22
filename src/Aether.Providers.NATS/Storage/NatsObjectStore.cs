using Aether.Abstractions.Storage;
using RickDotNet.Base;

namespace Aether.Providers.NATS.Storage;

public class NatsObjectStore : IStore
{

    public ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default) => throw new NotImplementedException();
    public ValueTask<Result<T>> Get<T>(string id, CancellationToken token) => throw new NotImplementedException();

    public ValueTask<Result<AetherData>> Upsert(string id, AetherData data, CancellationToken token = default) => throw new NotImplementedException();
    public ValueTask<Result<T>> Upsert<T>(string id, T data, CancellationToken token = default) => throw new NotImplementedException();

    public ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default) => throw new NotImplementedException();

    public ValueTask<Result<IEnumerable<string>>> ListKeys(CancellationToken token = default) => throw new NotImplementedException();
}

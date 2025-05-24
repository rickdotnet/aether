using Aether.Abstractions.Storage;
using NATS.Client.KeyValueStore;
using RickDotNet.Base;
using RickDotNet.Extensions.Base;

namespace Aether.Providers.NATS.Storage;

public class NatsKvStore : IStore
{
    private INatsKVStore kvStore = null!;

    public NatsKvStore(string bucketName, INatsKVContext kvContext)
    {
        // https://github.com/stebet/Stebet.Nats.DistributedCache/blob/72749defc8db1095eda596e2f0f0e46ba1f4f8a0/src/Stebet.Nats.DistributedCache/NatsDistributedCache.cs#L29
        var createOrUpdateTask = Task.Run(async () => { kvStore = await kvContext.CreateOrUpdateStoreAsync(new NatsKVConfig(bucketName)).ConfigureAwait(false); });

        createOrUpdateTask.Wait();
        if (kvStore == null)
            throw new Exception("Failed to create or update NATS KV store", createOrUpdateTask.Exception);
    }

    public async ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default)
    {
        var result = await kvStore.TryGetEntryAsync<Memory<byte>>(id, cancellationToken: token);
        return result.Success
            ? Result.Success(new AetherData(result.Value.Value))
            : Result.Failure<AetherData>(result.Error);
    }

    public async ValueTask<Result<T>> Get<T>(string id, CancellationToken token)
    {
        var storeResult = await Get(id, token);
        var valueResult = storeResult.Select(d => d.As<T>() ?? default);

        return valueResult.ValueOrDefault() == null
            ? Result.Failure<T>("No value, buddy.")
            : valueResult!;
    }

    public async ValueTask<Result<AetherData>> Insert(string id, AetherData data, CancellationToken token = default)
    {
        await kvStore.PutAsync(id, data.Data, cancellationToken: token);
        return Result.Success(data);
    }

    public async ValueTask<Result<T>> Insert<T>(string id, T data, CancellationToken token = default)
    {
        var result = await Insert(id, AetherData.Serialize(data), token);
        return result.Select(d => d.As<T>() ?? data);
    }

    public async ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default)
    {
        var data = await Get(id, token);
        var result = await Result.TryAsync(async () =>
        {
            await kvStore.DeleteAsync(id, cancellationToken: token);
            return data;
        });

        return result;
    }

    public async ValueTask<Result<IEnumerable<string>>> ListKeys(CancellationToken token = default)
    {
        var keys = new List<string>();
        var watchOpts = new NatsKVWatchOpts { OnNoData = _ => new ValueTask<bool>(true) };
        await foreach (var key in kvStore.GetKeysAsync(watchOpts, token))
        {
            keys.Add(key);
        }

        keys.Sort();
        return keys;
    }

    public ValueTask<Result<IEnumerable<AetherData>>> List<TData>(FilterCriteria<TData>? filterCriteria = null,
        CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}

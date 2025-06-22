using Aether.Abstractions.Storage;
using Microsoft.Extensions.Caching.Memory;
using RickDotNet.Base;
using RickDotNet.Extensions.Base;

namespace Aether.Providers.Memory;

public class MemoryStore : IStore
{
    private readonly MemoryCache memoryCache = new(new MemoryCacheOptions());

    public ValueTask<Result<AetherData>> Get(string id, CancellationToken token)
        => ValueTask.FromResult(
            memoryCache.TryGetValue(id, out Memory<byte> data)
                ? Result.Success(new AetherData(data))
                : Result.Error<AetherData>($"No data found for id: {id}")
        );

    public async ValueTask<Result<T>> Get<T>(string id, CancellationToken token)
    {
        var storeResult = await Get(id, token);
        var valueResult = storeResult.Select(d => d.As<T>() ?? default);
        
        return valueResult.ValueOrDefault() == null 
            ? Result.Error<T>("No value, buddy.") 
            : valueResult!;
    }

    public ValueTask<Result<AetherData>> Upsert(string id, AetherData data, CancellationToken token = default)
    {
        memoryCache.Set(id, data.Data);
        return ValueTask.FromResult(Result.Success(data));
    }

    public async ValueTask<Result<T>> Upsert<T>(string id, T data, CancellationToken token = default)
    {
        var result = await Upsert(id, AetherData.Serialize(data), token);
        return result.Select(d => d.As<T>() ?? data);
    }

    public ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default)
    {
        if (!memoryCache.TryGetValue(id, out byte[]? data))
            return ValueTask.FromResult(Result.Error<AetherData>($"No data found for id: {id}"));

        memoryCache.Remove(id);
        return ValueTask.FromResult(Result.Success(new AetherData(data)));
    }

    public ValueTask<Result<IEnumerable<string>>> ListKeys(CancellationToken token = default)
    {
        IEnumerable<string> keys = memoryCache.GetKeys();
        return ValueTask.FromResult(Result.Success(keys));
    }
}

public static class MemoryCacheExtensions
{
    public static IReadOnlyList<string> GetKeys(this MemoryCache memoryCache) 
        => memoryCache.Keys.Select(o => o.ToString()!).ToList();
}
using Aether.Abstractions.Storage;
using Microsoft.Extensions.Caching.Memory;
using RickDotNet.Base;

namespace Aether.Providers.Memory;

public class InMemoryStore : IStore
{
    private readonly MemoryCache memoryCache = new(new MemoryCacheOptions());

    public ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default) 
        => ValueTask.FromResult(
            memoryCache.TryGetValue(id, out Memory<byte> data)
                ? Result.Success(new AetherData(data))
                : Result.Failure<AetherData>($"No data found for id: {id}")
        );

    public ValueTask<Result<AetherData>> Insert(string id, AetherData data, CancellationToken token = default)
    {
        memoryCache.Set(id, data.Data);
        return ValueTask.FromResult(Result.Success(data));
    }

    public ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default)
    {
        if (!memoryCache.TryGetValue(id, out byte[]? data))
            return ValueTask.FromResult(Result.Failure<AetherData>($"No data found for id: {id}"));

        memoryCache.Remove(id);
        return ValueTask.FromResult(Result.Success(new AetherData(data)));
    }

    public ValueTask<Result<IEnumerable<string>>> ListKeys(CancellationToken token = default)
    {
        IEnumerable<string> keys = memoryCache.GetKeys();
        return ValueTask.FromResult(Result.Success(keys));
    }


    public ValueTask<Result<IEnumerable<AetherData>>> List<TData>(FilterCriteria<TData>? filterCriteria = null,
        CancellationToken token = default)
    {
        var keys = memoryCache.GetKeys();
        var data = keys.Select(key => memoryCache.Get<Memory<byte>>(key)).Select(data => new AetherData(data)).ToList();

        if (filterCriteria == null)
            return ValueTask.FromResult(
                Result.Success(data.AsEnumerable()));

        var results = new List<AetherData>();
        var filter = filterCriteria.Filter.Compile();
        foreach (var item in data)
        {
            var temp = item.As<TData>();
            if (temp != null && filter(temp))
                results.Add(item);
        }

        return ValueTask.FromResult(
            Result.Success(results.AsEnumerable())
        );
    }
}

public static class MemoryCacheExtensions
{
    public static IReadOnlyList<string> GetKeys(this MemoryCache memoryCache)
    {
        IReadOnlyList<string> list = memoryCache.Keys.Select(o => o.ToString()!).ToList();
        return list;
    }
}
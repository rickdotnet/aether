using RickDotNet.Base;

namespace Aether.Abstractions.Storage;

public interface IDefaultStorageProvider : IStorageProvider
{
    public const string DefaultStoreName = "default"; // TODO: this value is mentioned in comments below;
    /// <summary>
    /// Returns the default provider
    /// </summary>
    /// <returns>The default provider</returns>
    IStorageProvider AsProvider();


    Result<IStorageProvider> GetStore(string storeName);
    Result<VoidResult> SetStore(string storeName, IStorageProvider provider);
}

public interface IStorageProvider
{
    // starting simple using string as key and AetherData as value
    // AetherData wraps byes and provides json serialization/deserialization
    ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default);
    ValueTask<Result<AetherData>> Insert(string id, AetherData data, CancellationToken token = default);
    ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default);
    ValueTask<Result<IEnumerable<string>>> ListKeys(CancellationToken token = default);
    ValueTask<Result<IEnumerable<AetherData>>> List<TData>(FilterCriteria<TData>? filterCriteria = null, CancellationToken token = default);
}

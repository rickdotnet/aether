using RickDotNet.Base;

namespace Aether.Abstractions.Storage;

public interface IDefaultStorageProvider : IStorageProvider
{
    /// <summary>
    /// Returns the default provider
    /// </summary>
    /// <returns>The default provider</returns>
    IStorageProvider AsProvider();

    /// <summary>
    /// Returns the provider with the specified key
    /// </summary>
    /// <param name="providerKey">The key of the provider</param>
    /// <returns>The provider with the specified key</returns>
    IStorageProvider GetProvider(string providerKey);

    /// <summary>
    /// Returns a provider of the specified type
    /// </summary>
    /// <typeparam name="TProvider"></typeparam>
    /// <returns></returns>
    TProvider GetProvider<TProvider>() where TProvider : IStorageProvider;
}

public interface IStorageProvider
{
    // starting simple using string as key and AetherData as value
    // AetherData wraps byes and provides json serialization/deserialization
    ValueTask<Result<AetherData>> Get(string id, CancellationToken token = default);
    ValueTask<Result<AetherData>> Insert(string id, AetherData data, CancellationToken token = default);
    ValueTask<Result<AetherData>> Delete(string id, CancellationToken token = default);
    
    // TODO: decide on if a key filter makes sense or not  
    ValueTask<Result<IEnumerable<string>>> ListKeys(FilterCriteria<string>? filterCriteria = null, CancellationToken token = default);
    ValueTask<Result<IEnumerable<AetherData>>> List<TData>(FilterCriteria<TData>? filterCriteria = null, CancellationToken token = default);
}

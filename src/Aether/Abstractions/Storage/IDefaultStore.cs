using RickDotNet.Base;

namespace Aether.Abstractions.Storage;

public interface IDefaultStore : IStore
{
    public const string DefaultStoreName = "default"; // TODO: this value is mentioned in comments below;

    /// <summary>
    /// Returns the default store
    /// </summary>
    /// <returns>The default store</returns>
    IStore AsStore();

    /// <summary>
    /// Gets a store by name
    /// </summary>
    /// <param name="storeName">The name of the store</param>
    /// <returns>A result containing the store, or failure message.</returns>
    Result<IStore> GetStore(string storeName);

    /// <summary>
    /// Sets a store by name
    /// </summary>
    /// <param name="storeName">The name of the store</param>
    /// <param name="store">The store to set</param>
    /// <returns>A result containing the store, or failure message.</returns>
    Result<Unit> SetStore(string storeName, IStore store);
}

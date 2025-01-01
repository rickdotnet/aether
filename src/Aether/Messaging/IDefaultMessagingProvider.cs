namespace Aether.Messaging;

public interface IDefaultMessagingProvider : IMessagingProvider
{
    /// <summary>
    /// Get a provider by key
    /// </summary>
    /// <param name="providerKey">The provider key</param>
    /// <returns>The provider</returns>
    IMessagingProvider GetProvider(object providerKey);
    
    /// <summary>
    /// Returns the default provider
    /// </summary>
    /// <returns>The default provider</returns>
    IMessagingProvider AsProvider();
}

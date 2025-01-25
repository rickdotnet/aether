using RickDotNet.Base;

namespace Aether.Abstractions.Providers;

public interface IDefaultMessageHub : IMessageHub
{
    /// <summary>
    /// Retrieves the messaging Hub associated with the specified Hub key.
    /// </summary>
    /// <returns> The messaging Hub associated with the specified key. Returns a failure if the Hub is not found.</returns>
    Result<IMessageHub> GetHub(string hubKey);
    
    /// <summary>
    /// Retrieves the messaging Hub using the specified type.FullName or type.Name as the Hub key.
    /// </summary>
    /// /// <returns> The messaging Hub associated with the specified type. Returns a failure if the Hub is not found.</returns>
    Result<IMessageHub> GetHub<T>() where T : IMessageHub;


    /// <summary>
    /// Returns the default messaging Hub.
    /// </summary>
    IMessageHub AsHub();
}

using RickDotNet.Base;

namespace Aether.Abstractions.Messaging;

public interface IDefaultMessageHub : IMessageHub
{
    public const string DefaultHubKey = "default"; // TODO: this value is mentioned in comments below;
    /// <summary>
    /// Retrieves the messaging Hub associated with the specified Hub key.
    /// </summary>
    /// <returns> The messaging Hub associated with the specified key. Returns a failure if the Hub is not found.</returns>
    Result<IMessageHub> GetHub(string hubKey);
    
    /// <summary>
    /// Sets the provided message hub at the specified key
    /// </summary>
    Result<Unit> SetHub(string hubKey, IMessageHub hub);

    /// <summary>
    /// Returns the default messaging Hub.
    /// </summary>
    IMessageHub AsHub();
}

namespace Aether.Abstractions.Hosting;

public interface IMessagingBuilder
{
    /// <summary>
    /// Replace the default hub with a custom hub.
    /// </summary>
    /// <param name="configure"></param>
    /// <returns></returns>
    public IMessagingBuilder AddHub(Action<IHubBuilder> configure);

    /// <summary>
    /// Add a custom hub.
    /// </summary>
    /// <param name="hubName"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public IMessagingBuilder AddHub(string hubName, Action<IHubBuilder> configure);
}
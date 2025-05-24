using Aether.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Abstractions.Hosting;

public interface IMessagingBuilder
{
    public Type DefaultHubType { get; }

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
    public IMessagingBuilder AddHub<T>(string hubName, Action<IHubBuilder> configure) where T : IMessageHub;
    
    /// <summary>
    /// Registers the services required for the hub to function.
    /// </summary>
    /// <param name="configureServices">Delegate to configure services with the <see cref="IServiceCollection"/>.</param>
    public void RegisterServices(Action<IServiceCollection> configureServices);
}
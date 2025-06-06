using Aether.Abstractions.Messaging;
using Aether.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Abstractions.Hosting;

public interface IHubBuilder
{
    public IHubBuilder AddEndpoint<T>(EndpointConfig endpointConfig, HandlerConfig? handlerConfig = null);

    public IHubBuilder AddEndpoint(EndpointConfig endpointConfig, Type endpointType, HandlerConfig? handlerConfig = null);

    public IHubBuilder AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler, HandlerConfig? handlerConfig = null);

    /// <summary>
    /// Registers the services required for the hub to function.
    /// </summary>
    /// <param name="configureServices">Delegate to configure services with the <see cref="IServiceCollection"/>.</param>
    public void RegisterServices(Action<IServiceCollection> configureServices);
}

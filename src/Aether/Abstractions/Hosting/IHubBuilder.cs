using Aether.Abstractions.Messaging.Configuration;
using Aether.Abstractions.Providers;
using Aether.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Abstractions.Hosting;

public interface IHubBuilder
{
    public IHubBuilder AddEndpoint<T>(EndpointConfig endpointConfig);

    public IHubBuilder AddEndpoint(Type endpointType, EndpointConfig endpointConfig);

    public IHubBuilder AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler);

    /// <summary>
    /// Registers the services required for the hub to function.
    /// </summary>
    /// <param name="configureServices">Delegate to configure services with the <see cref="IServiceCollection"/>.</param>
    /// <typeparam name="TSubscriptionProvider">The type of the subscription provider.</typeparam>
    /// <typeparam name="TPublisherProvider">The type of the publisher provider.</typeparam>
    public void RegisterServices<TSubscriptionProvider, TPublisherProvider>(Action<IServiceCollection> configureServices)
        where TSubscriptionProvider : class, ISubscriptionProvider
        where TPublisherProvider : class, IPublisherProvider;
}

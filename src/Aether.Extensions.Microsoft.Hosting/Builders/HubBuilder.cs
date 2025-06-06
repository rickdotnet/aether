using Aether.Abstractions.Hosting;
using Aether.Abstractions.Messaging;
using Aether.Extensions.Microsoft.Hosting.Messaging;
using Aether.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Extensions.Microsoft.Hosting.Builders;

internal class HubBuilder : IHubBuilder
{
    private readonly AetherBuilder aetherBuilder;
    public HubRegistration HubRegistration { get; }

    public HubBuilder(string hubName, Type hubType, AetherBuilder aetherBuilder)
    {
        this.aetherBuilder = aetherBuilder;
        HubRegistration = new(hubName, hubType);
    }

    public IHubBuilder AddEndpoint<T>(EndpointConfig endpointConfig, HandlerConfig? handlerConfig = null)
    {
        EndpointRegistration registration = new(endpointConfig, typeof(T), handlerConfig);
        HubRegistration.AddRegistration(registration);
        return this;
    }

    public IHubBuilder AddEndpoint(EndpointConfig endpointConfig, Type endpointType, HandlerConfig? handlerConfig = null)
    {
        EndpointRegistration registration = new(endpointConfig, endpointType, handlerConfig);
        HubRegistration.AddRegistration(registration); 
        return this;
    }

    public IHubBuilder AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler, HandlerConfig? handlerConfig = null)
    {
        EndpointRegistration registration = new(endpointConfig, handler, handlerConfig);
        HubRegistration.AddRegistration(registration);
        return this;
    }

    public void RegisterServices(Action<IServiceCollection> configureServices)
    {
        aetherBuilder.RegisterServices(configureServices);
    }
}

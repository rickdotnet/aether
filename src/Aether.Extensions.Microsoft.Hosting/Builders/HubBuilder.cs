using Aether.Abstractions.Hosting;
using Aether.Abstractions.Messaging;
using Aether.Extensions.Microsoft.Hosting.Messaging;
using Aether.Messaging;
using Aether.Providers.Memory;
using Microsoft.Extensions.DependencyInjection;
using RickDotNet.Extensions.Base;

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
    
    public IHubBuilder AddEndpoint<T>(EndpointConfig endpointConfig)
    {
        HubRegistration.AddRegistration<T>(endpointConfig);
        return this;
    }

    public IHubBuilder AddEndpoint(EndpointConfig endpointConfig, Type endpointType)
    {
        HubRegistration.AddRegistration(endpointType, endpointConfig);
        return this;
    }

    public IHubBuilder AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
    {
        HubRegistration.AddRegistration(endpointConfig, handler);
        return this;
    }

    public void RegisterServices(Action<IServiceCollection> configureServices)
    {
        aetherBuilder.RegisterServices(configureServices);
    }
}

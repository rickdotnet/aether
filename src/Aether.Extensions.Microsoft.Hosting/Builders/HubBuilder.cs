using Aether.Abstractions.Hosting;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Abstractions.Providers;
using Aether.Messaging;
using Microsoft.Extensions.DependencyInjection;
using RickDotNet.Extensions.Base;

namespace Aether.Extensions.Microsoft.Hosting.Builders;

internal class HubBuilder : IHubBuilder
{
    private readonly AetherBuilder aetherBuilder;
    private readonly HubRegistration hubRegistration;

    public HubBuilder(AetherBuilder aetherBuilder, string hubName)
    {
        this.aetherBuilder = aetherBuilder;
        hubRegistration = new(hubName);
    }

    internal void Build()
    {
        var valid = hubRegistration.Validate();
        valid.OnError(error => throw new InvalidOperationException(error));

        aetherBuilder.RegisterServices(services => services.AddSingleton(hubRegistration));
    }

    public IHubBuilder AddEndpoint<T>(EndpointConfig endpointConfig)
    {
        hubRegistration.AddRegistration<T>(endpointConfig);
        return this;
    }

    public IHubBuilder AddEndpoint(Type endpointType, EndpointConfig endpointConfig)
    {
        hubRegistration.AddRegistration(endpointType, endpointConfig);
        return this;
    }

    public IHubBuilder AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
    {
        hubRegistration.AddRegistration(endpointConfig, handler);
        return this;
    }

    public void RegisterServices<TSubscriptionProvider, TPublisherProvider>(Action<IServiceCollection> configureServices)
        where TSubscriptionProvider : class, ISubscriptionProvider
        where TPublisherProvider : class, IPublisherProvider
    {
        hubRegistration.SetProviders<TSubscriptionProvider, TPublisherProvider>();
        aetherBuilder.RegisterServices(configureServices);
    }

}
using Aether.Abstractions.Hosting;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Extensions.Microsoft.Hosting.Messaging;
using Aether.Messaging;
using Aether.Providers.Memory;
using Microsoft.Extensions.DependencyInjection;
using RickDotNet.Extensions.Base;

namespace Aether.Extensions.Microsoft.Hosting.Builders;

internal class HubBuilder : IHubBuilder
{
    private readonly AetherBuilder aetherBuilder;
    private readonly HubRegistration hubRegistration;
    bool registerServicesCalled = false; // cheap way to default to UseMemory if no providers are set

    public HubBuilder(AetherBuilder aetherBuilder, string hubName)
    {
        this.aetherBuilder = aetherBuilder;
        hubRegistration = new(hubName);
    }

    internal void Build()
    {
        if (!registerServicesCalled)
            this.UseMemory();

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
        // if this is called, we don't want to default to UseMemory
        registerServicesCalled = true;

        hubRegistration.SetProviders<TSubscriptionProvider, TPublisherProvider>();
        aetherBuilder.RegisterServices(configureServices);
    }
}

public static class HubBuilderExtensions
{
    public static IHubBuilder UseMemory(this IHubBuilder hubBuilder)
    {
        hubBuilder.RegisterServices<MemoryHubProvider, MemoryHubProvider>(
            services =>
                services
                    .AddSingleton<MemoryHubProvider>()
        );

        return hubBuilder;
    }

    public static Task CreateEndpoint(this IMessageHub hub, EndpointRegistration registration)
    {
        return registration.IsHandler
            ? hub.AddHandler(registration.Config, registration.Handler!)
            : hub.AddEndpoint(registration.Config, registration.EndpointType!);
    }
}

using Aether.Abstractions.Messaging.Configuration;
using Aether.Abstractions.Providers;
using Aether.Messaging;
using RickDotNet.Base;

namespace Aether.Extensions.Microsoft.Hosting.MessageHub;

public sealed class HubRegistration
{
    private readonly List<EndpointRegistration> endpointRegistrations = new();
    public IReadOnlyList<EndpointRegistration> EndpointRegistrations => endpointRegistrations;
    public string HubName { get; }
    public Type? SubscriptionProviderType { get; private set; }

    public Type? PublisherProviderType { get; private set; }

    public HubRegistration(string hubName)
    {
        HubName = hubName;
    }

    public Result<bool> Validate()
    {
        return SubscriptionProviderType != null && PublisherProviderType != null
            ? true
            : Result.Failure<bool>("Missing provider type(s)");
    }

    public void AddRegistration<T>(EndpointConfig config)
        => endpointRegistrations.Add(EndpointRegistration.From<T>(config));

    public void AddRegistration(Type endpointType, EndpointConfig endpointConfig)
        => endpointRegistrations.Add(EndpointRegistration.From(endpointConfig, endpointType));

    public void AddRegistration(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
        => endpointRegistrations.Add(EndpointRegistration.From(endpointConfig, handler));

    public void SetProviders<TSubscriptionProvider, TPublisherProvider>()
        where TSubscriptionProvider : ISubscriptionProvider
        where TPublisherProvider : IPublisherProvider
    {
        SubscriptionProviderType = typeof(TSubscriptionProvider);
        PublisherProviderType = typeof(TPublisherProvider);
    }
}
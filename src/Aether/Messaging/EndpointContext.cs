using Aether.Abstractions.Messaging.Configuration;
using Aether.Abstractions.Providers;

namespace Aether.Messaging;

public class EndpointContext
{
    public EndpointConfig EndpointConfig { get; }
    public ISubscriptionProvider SubscriptionProvider { get; }
    public Type? EndpointType { get; }
    public IEndpointProvider? EndpointProvider { get; }
    public Func<MessageContext, CancellationToken, Task>? Handler { get; }

    public EndpointContext(EndpointConfig endpointConfig,
        ISubscriptionProvider subscriptionProvider,
        Type? endpointType = null,
        IEndpointProvider? endpointProvider = null,
        Func<MessageContext, CancellationToken, Task>? handler = null)
    {
        EndpointConfig = endpointConfig;
        SubscriptionProvider = subscriptionProvider;
        EndpointType = endpointType;
        EndpointProvider = endpointProvider;
        Handler = handler;
    }
}

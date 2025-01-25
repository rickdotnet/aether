using Aether.Messaging;
using Aether.Messaging.Configuration;

namespace Aether.Subscriptions;

public class Subscription
{
    public SubscriptionConfig Config { get; }
    public Func<MessageContext, CancellationToken, Task> Handler { get; }

    public Subscription(SubscriptionConfig config, Func<MessageContext, CancellationToken, Task> handler)
    {
        Config = config;
        Handler = handler;
    }
    
    public static Subscription ForEndpoint<T>(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
    {
        var endpointType = typeof(T);
        var subscriptionConfig = SubscriptionConfig.ForEndpoint(endpointConfig, endpointType);
        
        // get this from synchronous endpoint
        
        return new Subscription(subscriptionConfig, handler);
    }
}

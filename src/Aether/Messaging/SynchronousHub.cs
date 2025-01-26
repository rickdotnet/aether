using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Abstractions.Providers;

namespace Aether.Messaging;

public class SynchronousHub : IMessageHub
{
    private readonly ISubscriptionProvider subProvider;
    private readonly IPublisherProvider publisherProvider;
    private GenericEndpointProvider? genericEndpointProvider;

    public SynchronousHub(ISubscriptionProvider subProvider, IPublisherProvider publisherProvider)
    {
        this.subProvider = subProvider;
        this.publisherProvider = publisherProvider;
    }

    public IAetherEndpoint AddEndpoint<T>(EndpointConfig endpointConfig)
    {
        var endpointContext = new EndpointContext(endpointConfig, subProvider, endpointType: typeof(T));
        return new SynchronousEndpoint(endpointContext);
    }

    public IAetherEndpoint AddEndpoint(Type endpointType, EndpointConfig endpointConfig)
    {
        var endpointContext = new EndpointContext(endpointConfig, subProvider, endpointType: endpointType);
        return new SynchronousEndpoint(endpointContext);
    }

    public IAetherEndpoint AddEndpoint<T>(EndpointConfig endpointConfig, T instance) where T : class
    {
        genericEndpointProvider ??= new();
        genericEndpointProvider.AddService(instance); 
        
        var endpointContext = new EndpointContext(endpointConfig, subProvider, endpointType: typeof(T), endpointProvider: genericEndpointProvider);
        return new SynchronousEndpoint(endpointContext);
    }

    public IAetherEndpoint AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
    {
        var endpointContext = new EndpointContext(endpointConfig, subProvider, handler: handler);
        return new SynchronousEndpoint(endpointContext);
    }

    public IPublisher CreatePublisher(EndpointConfig endpointConfig)
        => CreatePublisher(endpointConfig.ToPublishConfig());

    public IPublisher CreatePublisher(PublishConfig publishConfig)
    {
        return new DefaultPublisher(publishConfig, publisherProvider);
    }
}

using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Abstractions.Providers;
using RickDotNet.Extensions.Base;

namespace Aether.Messaging;

/// <summary>
/// This is the current bottleneck of the system. It is the main entry point for creating endpoints and publishers.
/// </summary>
/// <remarks>This is on the hot-path to get reworked</remarks>
public class SynchronousHub : IMessageHub
{
    private readonly ISubscriptionProvider subProvider;
    private readonly IPublisherProvider publisherProvider;
    
    // for DI services
    private readonly IEndpointProvider? endpointProvider;
    
    // for capturing passed in instances
    private GenericEndpointProvider? genericEndpointProvider;

    public SynchronousHub(ISubscriptionProvider subProvider, IPublisherProvider publisherProvider, IEndpointProvider? endpointProvider = null)
    {
        this.subProvider = subProvider;
        this.publisherProvider = publisherProvider;
        this.endpointProvider = endpointProvider;
    }

    public IAetherEndpoint AddEndpoint<T>(EndpointConfig endpointConfig)
    {
        var endpointContext = new EndpointContext(endpointConfig, subProvider, endpointType: typeof(T), endpointProvider);
        return new SynchronousEndpoint(endpointContext);
    }

    public IAetherEndpoint AddEndpoint(EndpointConfig endpointConfig, Type endpointType)
    {
        var endpointContext = new EndpointContext(endpointConfig, subProvider, endpointType: endpointType, endpointProvider);
        return new SynchronousEndpoint(endpointContext);
    }

    public IAetherEndpoint AddEndpoint<T>(EndpointConfig endpointConfig, T instance) where T : class
    {
        genericEndpointProvider ??= new();
        var result = genericEndpointProvider.AddService(instance);
        
        // throwing here until we get results propagated more effectively
        result.OnError(error=> throw new Exception(error));
        
        
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

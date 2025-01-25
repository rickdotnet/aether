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
        var config = endpointConfig with { EndpointType = typeof(T) };
        return new SynchronousEndpoint(config, subProvider);
    }

    public IAetherEndpoint AddEndpoint(Type endpointType, EndpointConfig endpointConfig)
    {
        var config = endpointConfig with { EndpointType = endpointType };
        return new SynchronousEndpoint(config, subProvider);
    }

    public IAetherEndpoint AddEndpoint<T>(EndpointConfig endpointConfig, T instance) where T : class
    {
        var config = endpointConfig with { EndpointType = typeof(T) };
        genericEndpointProvider ??= new();
        genericEndpointProvider.AddService(instance); 
        
        return new SynchronousEndpoint(config, subProvider, endpointProvider: genericEndpointProvider);
    }

    public IAetherEndpoint AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
    {
        return new SynchronousEndpoint(endpointConfig, subProvider, handler: handler);
    }

    public IPublisher CreatePublisher(EndpointConfig endpointConfig)
        => CreatePublisher(endpointConfig.ToPublishConfig());

    public IPublisher CreatePublisher(PublishConfig publishConfig)
    {
        return new DefaultPublisher(publishConfig, publisherProvider);
    }

    // private EndpointConfig SetEndpointDefaults(EndpointConfig config, Type? endpointType = null) 
    //     => config with
    //     {
    //         InstanceId = config.InstanceId ?? aetherConfig.InstanceId,
    //         ConsumerName = config.ConsumerName 
    //                        ?? aetherConfig.DefaultConsumerName 
    //                        ?? throw new Exception("ConsumerName is required."),
    //         CreateMissingResources = config.InternalCreateMissingResources ?? aetherConfig.CreateMissingResources,
    //         AckStrategy = config.InternalAckStrategy ?? aetherConfig.AckStrategy,
    //         Namespace = config.Namespace ?? aetherConfig.DefaultNamespace,
    //         EndpointType = config.EndpointType ?? endpointType,
    //         SubscriptionProvider = config.SubscriptionProvider ?? defaultSubscriptionProvider,
    //         EndpointProvider = config.EndpointProvider ?? endpointProvider
    //     };
}

using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Abstractions.Providers;
using RickDotNet.Base;

namespace Aether.Messaging;

internal class DefaultMessageHub : IDefaultMessageHub
{
    // TODO: will revisit this, for now, just getting the default hub working
    private const string DefaultHubKey = "default"; // TODO: this value is mentioned in comments below;
    private readonly Dictionary<string, IMessageHub> hubs;
    private IMessageHub DefaultHub => hubs[DefaultHubKey];

    /// <summary>
    /// Create a new DefaultMessagingHub with the given default hub.
    /// </summary>
    public DefaultMessageHub(IMessageHub defaultHub)
    {
        hubs = new Dictionary<string, IMessageHub>
        {
            [DefaultHubKey] = defaultHub
        };
    }
    /// <summary>
    /// Create a new DefaultMessagingHub with the given hubs. 
    /// The default hub should be keyed "default" (defined by <see cref="DefaultHubKey"/>); 
    /// otherwise, the first hub will be used as the default.
    /// </summary>
    public DefaultMessageHub(Dictionary<string, IMessageHub> hubs)
    {
        // copy the dictionary
        this.hubs = hubs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (!this.hubs.ContainsKey(DefaultHubKey))
        {
            this.hubs[DefaultHubKey] = this.hubs.First().Value;
        }
    }

    /// <inheritdoc />
    public Result<IMessageHub> GetHub(string hubKey)
    {
        // result or failure
        return Result.Try(() => hubs[hubKey]);
    }

    /// <inheritdoc />
    public Result<IMessageHub> GetHub<T>() where T : IMessageHub
    {
        var type = typeof(T);
        var key = type.FullName ?? type.Name;
        
        // this will be result or failure
        return GetHub(key);
    }

    /// <inheritdoc />
    public IMessageHub AsHub() => hubs[DefaultHubKey];

    /// <inheritdoc />
    public IAetherEndpoint AddEndpoint<T>(EndpointConfig endpointConfig)
        => DefaultHub.AddEndpoint<T>(endpointConfig);

    /// <inheritdoc />
    public IAetherEndpoint AddEndpoint<T>(EndpointConfig endpointConfig, T instance) where T : class 
        => DefaultHub.AddEndpoint(endpointConfig, instance);

    /// <inheritdoc />
    public IAetherEndpoint AddEndpoint(Type endpointType, EndpointConfig endpointConfig)
        => DefaultHub.AddEndpoint(endpointType, endpointConfig);

    /// <inheritdoc />
    public IAetherEndpoint AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
        => DefaultHub.AddHandler(endpointConfig, handler);

    /// <inheritdoc />
    public IPublisher CreatePublisher(EndpointConfig endpointConfig)
        => DefaultHub.CreatePublisher(endpointConfig);

    /// <inheritdoc />
    public IPublisher CreatePublisher(PublishConfig publishConfig)
        => DefaultHub.CreatePublisher(publishConfig);

}

using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using RickDotNet.Base;

namespace Aether.Messaging;

internal class DefaultMessageHub : IDefaultMessageHub
{
    // TODO: will revisit this, for now, just getting the default hub working
    private readonly Dictionary<string, IMessageHub> hubs;
    private IMessageHub DefaultHub => hubs[IDefaultMessageHub.DefaultHubKey];

    /// <summary>
    /// Create a new DefaultMessagingHub with the given default hub.
    /// </summary>
    public DefaultMessageHub(IMessageHub defaultHub)
    {
        hubs = new Dictionary<string, IMessageHub>
        {
            [IDefaultMessageHub.DefaultHubKey] = defaultHub,
        };
    }

    public DefaultMessageHub(Dictionary<string, IMessageHub> hubs)
    {
        // copy the dictionary
        this.hubs = hubs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (!this.hubs.ContainsKey(IDefaultMessageHub.DefaultHubKey))
        {
            this.hubs[IDefaultMessageHub.DefaultHubKey] = this.hubs.First().Value;
        }
    }

    /// <inheritdoc />
    public Result<IMessageHub> GetHub(string hubKey)
    {
        // result or failure
        return Result.Try(() => hubs[hubKey]);
    }

    /// <inheritdoc />
    public Result<VoidResult> SetHub(string hubKey, IMessageHub hub)
    {
        return Result.Try(() =>
        {
            hubs[hubKey] = hub;    
        });
    }

    /// <inheritdoc />
    public IMessageHub AsHub() => hubs[IDefaultMessageHub.DefaultHubKey];

    /// <inheritdoc />
    public Task Start(CancellationToken cancellationToken) => DefaultHub.Start(cancellationToken);

    /// <inheritdoc />
    public Task AddEndpoint<T>(EndpointConfig endpointConfig)
        => DefaultHub.AddEndpoint<T>(endpointConfig);
    
    /// <inheritdoc />
    public Task AddEndpoint(EndpointConfig endpointConfig, Type endpointType)
        => DefaultHub.AddEndpoint(endpointConfig, endpointType);

    /// <inheritdoc />
    public Task AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
        => DefaultHub.AddHandler(endpointConfig, handler);

    /// <inheritdoc />
    public IPublisher CreatePublisher(EndpointConfig endpointConfig)
        => DefaultHub.CreatePublisher(endpointConfig);

    /// <inheritdoc />
    public IPublisher CreatePublisher(PublishConfig publishConfig)
        => DefaultHub.CreatePublisher(publishConfig);

}

using Aether.Abstractions.Messaging;
using RickDotNet.Base;

// ReSharper disable once CheckNamespace
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

    /// <summary>
    /// Create a new DefaultMessagingHub with the given hubs.
    /// </summary>
    /// <param name="hubs"></param>
    public DefaultMessageHub(Dictionary<string, IMessageHub> hubs)
    {
        this.hubs = hubs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (!this.hubs.ContainsKey(IDefaultMessageHub.DefaultHubKey))
        {
            this.hubs[IDefaultMessageHub.DefaultHubKey] = this.hubs.First().Value;
        }
    }

    /// <inheritdoc />
    public Result<IMessageHub> GetHub(string hubKey)
    {
        return Result.Try(() => hubs[hubKey]);
    }

    /// <inheritdoc />
    public Result<Unit> SetHub(string hubKey, IMessageHub hub) 
        => Result.Try(() => { hubs[hubKey] = hub; });

    /// <inheritdoc />
    public IMessageHub AsHub() => hubs[IDefaultMessageHub.DefaultHubKey];

    /// <inheritdoc />
    public void AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler, CancellationToken cancellationToken) 
        // TODO: need to handle cancellation token in hosted environment
        //       will likely add a background service and set it from there.
        //       but, will need to link it to the passed in cancellation token
        => DefaultHub.AddHandler(endpointConfig, handler, cancellationToken);  

    /// <inheritdoc />
    public Task<Result<Unit>> Send(AetherMessage message, CancellationToken cancellationToken) 
        => DefaultHub.Send(message, cancellationToken);

    /// <inheritdoc />
    public Task<Result<AetherData>> Request(AetherMessage message, CancellationToken cancellationToken) 
        => DefaultHub.Request(message, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        foreach (var hub in hubs.Values)
            await hub.DisposeAsync();
    }
}

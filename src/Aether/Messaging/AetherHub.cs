using System.Collections.Concurrent;
using System.Threading.Channels;
using Aether.Abstractions.Messaging;
using RickDotNet.Base;

namespace Aether.Messaging;

public sealed class AetherHub : IMessageHub
{
    private readonly IMessageHub innerHub;
    private readonly ConcurrentDictionary<string, Func<MessageContext, CancellationToken, Task>> handlers = new();
    private readonly ConcurrentDictionary<string, Channel<MessageContext>> channels = new();
    private readonly CancellationTokenSource cts = new();

    public AetherHub(IMessageHub innerHub)
    {
        this.innerHub = innerHub;
    }

    public static AetherHub For(IMessageHub innerHub) => new(innerHub);

    public void AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        AddHandler(endpointConfig, HandlerConfig.Default, handler, cancellationToken);
    }

    public void AddHandler(
        EndpointConfig endpointConfig,
        HandlerConfig handlerConfig,
        Func<MessageContext, CancellationToken, Task> handler,
        CancellationToken cancellationToken
    )
    {
        if (!handlerConfig.Validate())
            throw new ArgumentException("Invalid handler configuration", nameof(handlerConfig));

        var uniqueId = endpointConfig.EndpointId ?? Guid.NewGuid().ToString("N");
        if (!handlers.TryAdd(uniqueId, handler))
            return;

        var useSingleReader = handlerConfig.MaxProcessors <= 1;
        var channel = Channel.CreateBounded<MessageContext>(
            new BoundedChannelOptions(handlerConfig.MessageBufferCapacity)
            {
                SingleReader = useSingleReader,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        var channelAdded = channels.TryAdd(uniqueId, channel);
        if (!channelAdded)
            return;

        innerHub.AddHandler(endpointConfig, async (context, ct) => await channel.Writer.WriteAsync(context, ct), cancellationToken);
        for (var i = 0; i < handlerConfig.MaxProcessors; i++)
        {
            Task.Run(() => ProcessChannel(uniqueId, channel, cts.Token), cancellationToken);
        }
    }

    public Task<Result<Unit>> Send(AetherMessage message, CancellationToken cancellationToken = default)
        => innerHub.Send(message, cancellationToken);

    public Task<Result<AetherData>> Request(AetherMessage message, CancellationToken cancellationToken)
        => innerHub.Request(message, cancellationToken);

    private async Task ProcessChannel(string subject, Channel<MessageContext> channel, CancellationToken cancellationToken)
    {
        if (!handlers.TryGetValue(subject, out var handler))
            return;

        await foreach (var context in channel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await handler(context, cancellationToken);
            }
            catch (Exception ex)
            {
                // TODO: where the logging at?
                Console.WriteLine($"Error processing message from {subject}: {ex}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await cts.CancelAsync();
        cts.Dispose();

        foreach (var channel in channels.Values)
            channel.Writer.Complete();

        await innerHub.DisposeAsync();
    }
}

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
        // for now, only allowing one handler per subject
        var subject = endpointConfig.FullSubject;
        if (!handlers.TryAdd(subject, handler))
            return;

        // TODO: configurable channel options
        var channel = Channel.CreateBounded<MessageContext>(
            new BoundedChannelOptions(1000)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        channels.TryAdd(subject, channel);
        innerHub.AddHandler(endpointConfig, async (context, ct) => await channel.Writer.WriteAsync(context, ct), cancellationToken);

        Task.Run(() => ProcessChannel(subject, channel, cts.Token), cancellationToken);
    }

    public Task<Result<VoidResult>> Send(AetherMessage message, CancellationToken cancellationToken = default) 
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

using System.Threading.Channels;
using Aether.Abstractions.Messaging;
using Aether.Messaging;

namespace Aether.Providers.Memory;

// in-memory only, TODO: we might have some bacon here
internal record SubscriptionOptions
{
    // provide a spot for things like retry policy/dead-letter actions, etc
    public TimeSpan HandlerTimeout { get; init; } = TimeSpan.FromSeconds(60);
    public int? MaxCapacity { get; init; }
}

internal class MemorySubscription : ISubscription
{
    public ChannelWriter<MessageContext> Writer { get; }
    private readonly Func<MessageContext, CancellationToken, Task> handler;
    private readonly SubscriptionOptions? subscriptionOptions;
    private readonly Channel<MessageContext> channel;
    private Action disposeAction = () => { };


    public MemorySubscription(Func<MessageContext, CancellationToken, Task> handler, SubscriptionOptions? subscriptionOptions = null)
    {
        this.handler = handler;
        this.subscriptionOptions = subscriptionOptions;

        channel = subscriptionOptions?.MaxCapacity is { } maxCapacity and > 0
            ? Channel.CreateBounded<MessageContext>(new BoundedChannelOptions(maxCapacity))
            : Channel.CreateUnbounded<MessageContext>();

        Writer = channel.Writer;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        await foreach (var context in channel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var timeoutToken = subscriptionOptions?.HandlerTimeout is { } handlerTimeout
                    ? new CancellationTokenSource(handlerTimeout).Token
                    : CancellationToken.None;

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken);
                
                cancellationToken.ThrowIfCancellationRequested();
                await handler(context, linkedCts.Token);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Message handling failed: {ex.Message}");
            }
        }
    }

    public void OnDispose(Action onDispose)
    {
        disposeAction = onDispose;
    }

    public async ValueTask DisposeAsync()
    {
        disposeAction();
        channel.Writer.TryComplete(); // Close the Writer

        // make sure the reader finishes processing 
        while (await channel.Reader.WaitToReadAsync())
        {
            channel.Reader.TryRead(out _); // drain it
        }
    }
}

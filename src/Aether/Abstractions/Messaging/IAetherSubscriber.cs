namespace Aether.Abstractions.Messaging;

public interface IAetherSubscriber : IAsyncDisposable
{
    Task StartSubscription(CancellationToken cancellationToken);
}
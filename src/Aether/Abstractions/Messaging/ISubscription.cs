namespace Aether.Abstractions.Messaging;

public interface ISubscription : IAsyncDisposable
{
    Task Subscribe(CancellationToken cancellationToken);
}

namespace Aether.Abstractions.Messaging;

public interface IAetherEndpoint : IAsyncDisposable
{
    Task StartEndpoint(CancellationToken cancellationToken);
}
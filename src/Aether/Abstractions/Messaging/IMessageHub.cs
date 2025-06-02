using Aether.Messaging;
using RickDotNet.Base;

namespace Aether.Abstractions.Messaging;

public interface IMessageHub : IAsyncDisposable
{
    /// <summary>
    /// Registers a message handler for the given configuration, using the provided handler function.
    /// </summary>
    /// <param name="endpointConfig">The configuration for the endpoint to handle messages from.</param>
    /// <param name="handler">
    /// A function to handle messages. Accepts a <see cref="MessageContext"/> and a <see cref="CancellationToken"/>.
    /// </param>
    public void AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the specified endpoint.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<Unit>> Send(AetherMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a message from the specified endpoint and waits for a response.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<AetherData>> Request(AetherMessage message, CancellationToken cancellationToken);
}

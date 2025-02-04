using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;

namespace Aether.Abstractions.Messaging;

public interface IMessageHub
{
    /// <summary>
    /// Starts the message hub and subscribes to all configured endpoints.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>This method is a NoOp when created using the Hosting library</remarks>
    public Task Start(CancellationToken cancellationToken);
    /// <summary>
    /// Adds a new endpoint for the specified type using the given configuration.
    /// </summary>
    public Task AddEndpoint<T>(EndpointConfig endpointConfig);
    
    /// <summary>
    /// Adds a new endpoint for the specified endpoint type using the given configuration.
    /// </summary>
    public Task AddEndpoint(EndpointConfig endpointConfig, Type endpointType);

    /// <summary>
    /// Registers a message handler for the given configuration, using the provided handler function.
    /// </summary>
    /// <param name="endpointConfig">The configuration for the endpoint to handle messages from.</param>
    /// <param name="handler">
    /// A function to handle messages. Accepts a <see cref="MessageContext"/> and a <see cref="CancellationToken"/>.
    /// </param>
    public Task AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler);

    /// <summary>
    /// Creates a publisher for the given endpoint configuration.
    /// </summary>
    public IPublisher CreatePublisher(EndpointConfig endpointConfig);

    /// <summary>
    /// Creates a publisher with the specified publishing configuration.
    /// </summary>
    public IPublisher CreatePublisher(PublishConfig publishConfig);
}

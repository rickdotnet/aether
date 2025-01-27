using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;

namespace Aether.Abstractions.Providers;

public interface IMessageHub
{
    /// <summary>
    /// Adds a new endpoint for the specified type using the given configuration.
    /// </summary>
    public IAetherEndpoint AddEndpoint<T>(EndpointConfig endpointConfig);
    
    /// <summary>
    /// Adds a new endpoint for the specified endpoint type using the given configuration.
    /// </summary>
    public IAetherEndpoint AddEndpoint(EndpointConfig endpointConfig, Type endpointType);

    /// <summary>
    /// Adds a new endpoint for the specified type using the given configuration and instance.
    /// </summary>
    public IAetherEndpoint AddEndpoint<T>(EndpointConfig endpointConfig, T instance) where T : class;

    /// <summary>
    /// Registers a message handler for the given configuration, using the provided handler function.
    /// </summary>
    /// <param name="endpointConfig">The configuration for the endpoint to handle messages from.</param>
    /// <param name="handler">
    /// A function to handle messages. Accepts a <see cref="MessageContext"/> and a <see cref="CancellationToken"/>.
    /// </param>
    public IAetherEndpoint AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler);

    /// <summary>
    /// Creates a publisher for the given endpoint configuration.
    /// </summary>
    public IPublisher CreatePublisher(EndpointConfig endpointConfig);

    /// <summary>
    /// Creates a publisher with the specified publishing configuration.
    /// </summary>
    public IPublisher CreatePublisher(PublishConfig publishConfig);
}

using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;

namespace Aether.Extensions.Microsoft.Hosting;

public sealed class EndpointRegistration
{
    public EndpointConfig Config { get; }
    public bool IsHandler => Handler is not null;
    public Type? EndpointType { get; }
    
    public Func<MessageContext, CancellationToken, Task>? Handler { get; }

    public EndpointRegistration(EndpointConfig config, Type endpointType)
    {
        Config = config;
        EndpointType = endpointType;
    }

    public EndpointRegistration(EndpointConfig config, Func<MessageContext, CancellationToken, Task> handler)
    {
        Config = config;
        Handler = handler;
    }

    public static EndpointRegistration From<T>(EndpointConfig config)
        => new(config, typeof(T));

    public static EndpointRegistration From(EndpointConfig config, Type type)
        => new(config, type);

    public static EndpointRegistration From(EndpointConfig config, Func<MessageContext, CancellationToken, Task> handler)
        => new(config, handler);
}

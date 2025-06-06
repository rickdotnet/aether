using Aether.Abstractions.Messaging;
using Aether.Messaging;

namespace Aether.Extensions.Microsoft.Hosting.Messaging;

public sealed class EndpointRegistration
{
    public EndpointConfig Config { get; }
    public bool IsHandler => Handler is not null;
    public Type? EndpointType { get; }
    public HandlerConfig HandlerConfig { get; init; } = HandlerConfig.Default;

    public Func<MessageContext, CancellationToken, Task>? Handler { get; }

    public EndpointRegistration(EndpointConfig config, Type endpointType, HandlerConfig? handlerConfig = null)
    {
        Config = config;
        EndpointType = endpointType;

        if (handlerConfig is not null)
            HandlerConfig = handlerConfig;
    }

    public EndpointRegistration(EndpointConfig config, Func<MessageContext, CancellationToken, Task> handler, HandlerConfig? handlerConfig = null)
    {
        Config = config;
        Handler = handler;

        if (handlerConfig is not null)
            HandlerConfig = handlerConfig;
    }

    public bool Validate()
    {
        return EndpointType != null || Handler != null;
    }
}

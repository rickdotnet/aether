using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;

namespace ConsoleDemo.Endpoints;

public class InstanceEndpoint : IHandle<SomethingHappenedCommand>
{
    public static readonly EndpointConfig EndpointConfig = new()
    {
        EndpointName = "Instance Endpoint",
        Subject = "instance.endpoint",
    };

    public Task Handle(SomethingHappenedCommand message, MessageContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Instance Endpoint - {message.Message}");
        return Task.CompletedTask;
    }

    public Task Handle(MessageContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Instance Endpoint (Fallback) - {context.Headers[MessageHeader.MessageTypeMapping]}");
        return Task.CompletedTask;
    }
}
using Aether.Abstractions.Messaging;
using Aether.Messaging;
using Aether.Messaging.Configuration;

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
}

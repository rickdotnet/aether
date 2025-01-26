using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;

namespace ConsoleDemo.Endpoints;

public class StaticEndpoint
{
    public static EndpointConfig EndpointConfig => new()
    {
        EndpointName = "Static Endpoint",
        Subject = "static.endpoint",
    };
    
    public static Task Handle(MessageContext context, CancellationToken cancellationToken)
    {
        var theThing = context.Data.As<SomethingHappenedCommand>()!;
        Console.WriteLine($"Static Endpoint -  {theThing.Message}");
        
        return Task.CompletedTask;
    }
    
}

using Aether.Abstractions.Messaging;
using Aether.Messaging;

namespace ConsoleDemo.Endpoints;

public class WildCardEndpoint : IHandle<SomethingHappenedCommand>
{
    
    public Task Handle(MessageContext context, CancellationToken cancellationToken)
    {
        // var theThing = context.Data.As<SomethingHappenedCommand>()!;
        // Console.WriteLine($"WildCard Endpoint -  {theThing.Message}");

        Console.WriteLine("Wildcard endpoint");
        
        return Task.CompletedTask;
    }

    public Task Handle(SomethingHappenedCommand message, MessageContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"WildCard Endpoint -  {message.Message}");
        return Task.CompletedTask;
    }
}

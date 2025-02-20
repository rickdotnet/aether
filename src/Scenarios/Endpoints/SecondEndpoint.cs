using Aether;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;

namespace Scenarios.Endpoints;

public class SecondEndpoint : IHandle<SomethingHappenedCommand>, IReplyTo<Memory.TestRequest, string>
{
    public static readonly EndpointConfig EndpointConfig = new()
    {
        EndpointName = "Second Endpoint",
        Subject = "second.endpoint",
    };

    public Task Handle(SomethingHappenedCommand message, MessageContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Second Endpoint - {message.Message}");
        return Task.CompletedTask;
    }

    public Task Handle(MessageContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Second Endpoint (Fallback) - {context.Headers[MessageHeader.MessageTypeMapping]}");

        if (context.ReplyAvailable)
            context.Reply(AetherData.Serialize("I'm the fallback"), cancellationToken);
        else
            Console.WriteLine("No reply function available");

        return Task.CompletedTask;
    }

    public Task<string> Handle(Memory.TestRequest message, MessageContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Second Endpoint - TestRequest - Normal");
        return Task.FromResult("Success From Second Endpoint - Normal");
    }
}

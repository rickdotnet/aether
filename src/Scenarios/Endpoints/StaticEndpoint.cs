using Aether;
using Aether.Abstractions.Messaging;
using Aether.Messaging;

namespace Scenarios.Endpoints;

public class StaticEndpoint
{
    public static EndpointConfig EndpointConfig => new("static.endpoint");

    public static async Task Handle(MessageContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        var action = context.Headers[MessageHeader.MessageAction].FirstOrDefault();
        var message = action switch
        {
            "request" => context.Data.As<TestRequest>()?.Message,
            "command" => context.Data.As<SomethingHappenedCommand>()?.Message,
            _ => "Unknown action"
        };
        
        Console.WriteLine($"{context.Subject} - {message}");

        if (context.ReplyAvailable)
            await context.Reply(AetherData.Serialize("that test passed, babeh"), cancellationToken);
    }

}

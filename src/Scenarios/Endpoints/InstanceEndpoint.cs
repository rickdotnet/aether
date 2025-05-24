using Aether.Abstractions.Messaging;
using Aether.Messaging;

namespace Scenarios.Endpoints;

public class InstanceEndpoint : IHandle<SomethingHappenedCommand> //, IReplyTo<Memory.TestRequest, string>
{
    public static readonly EndpointConfig EndpointConfig = EndpointConfig.For("instance.endpoint").WithName("Instance Endpoint");
    public Task Handle(SomethingHappenedCommand message, MessageContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Instance Endpoint - {message.Message}");
        return Task.CompletedTask;
    }

    // these simulate requests w/ no methods to reply
    // leaving comments  until I get tests created
    
    // public Task Handle(MessageContext context, CancellationToken cancellationToken)
    // {
    //     Console.WriteLine($"Instance Endpoint (Fallback) - {context.Headers[MessageHeader.MessageTypeMapping]}");
    //
    //     if (context.ReplyAvailable)
    //         context.Reply(AetherData.Serialize("I'm the fallback"), cancellationToken);
    //     else
    //         Console.WriteLine("No reply function available");
    //
    //     return Task.CompletedTask;
    // }
    //
    // public Task<string> Handle(Memory.TestRequest message, MessageContext context, CancellationToken cancellationToken = default)
    // {
    //     Console.WriteLine("Instance Endpoint - TestRequest - Normal");
    //     return Task.FromResult("Success From Instance Endpoint");
    // }
    public Task Handle(MessageContext context, CancellationToken cancellationToken) => throw new NotImplementedException();
}

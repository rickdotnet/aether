using Aether.Messaging;

namespace Aether.Abstractions.Messaging;

public interface IHandle
{
    
}
public interface IHandle<in T> : IHandle where T : ICommand
{
    Task Handle(T message, MessageContext context, CancellationToken cancellationToken);
}
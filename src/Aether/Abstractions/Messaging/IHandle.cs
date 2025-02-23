using Aether.Messaging;

namespace Aether.Abstractions.Messaging;

public interface IHandle
{
    Task Handle(MessageContext context, CancellationToken cancellationToken);
}

public interface IHandle<in T> where T : ICommand
{
    Task Handle(T message, MessageContext context, CancellationToken cancellationToken);
}
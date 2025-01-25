using Aether.Messaging;

namespace Aether.Abstractions.Messaging;

public interface IListenFor
{
    
}
public interface IListenFor<in TEvent> : IListenFor where TEvent : IEvent
{
    public Task Handle(TEvent message, MessageContext context, CancellationToken cancellationToken = default);
}

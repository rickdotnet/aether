namespace Aether.Abstractions.Messaging;

public interface IPublisher
{
    Task Send<TType>(AetherData data, string? action, CancellationToken cancellationToken = default);
    
    Task Send<TCommand>(TCommand commandMessage, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    Task Broadcast<TEvent>(TEvent eventMessage, CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    Task<TResponse?> Request<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;
}
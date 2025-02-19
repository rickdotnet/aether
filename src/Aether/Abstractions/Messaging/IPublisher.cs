using RickDotNet.Base;

namespace Aether.Abstractions.Messaging;

public interface IPublisher
{
    Task Send<TCommand>(TCommand commandMessage, CancellationToken cancellationToken = default)
        where TCommand : ICommand;
    Task Send<TType>(AetherData data, CancellationToken cancellationToken = default);
    
    Task Broadcast<TEvent>(TEvent eventMessage, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
    Task Broadcast<TType>(AetherData data, CancellationToken cancellationToken = default);

    Task<Result<AetherData>> Request<TType>(AetherData requestData, CancellationToken cancellationToken);
    Task<TResponse?> Request<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;
}
using RickDotNet.Base;

namespace Aether.Abstractions.Messaging;

public interface IPublisher
{
    Task Send<TCommand>(TCommand commandMessage, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    Task Send<TCommand>(AetherData data, CancellationToken cancellationToken = default);

    Task Broadcast<TEvent>(TEvent eventMessage, CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    Task Broadcast<TEvent>(AetherData data, CancellationToken cancellationToken = default);

    Task<Result<AetherData>> Request<TRequest>(AetherData requestData, CancellationToken cancellationToken);

    Task<Result<AetherData>> Request<TRequest>(AetherData<TRequest> requestData, CancellationToken cancellationToken);

    Task<TResponse?> Request<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;
}
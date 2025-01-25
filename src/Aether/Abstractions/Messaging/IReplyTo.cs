using Aether.Messaging;

namespace Aether.Abstractions.Messaging;

public interface IReplyTo<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest message, MessageContext context, CancellationToken cancellationToken = default);
}
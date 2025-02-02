using Aether.Messaging;

namespace Aether.Abstractions.Messaging.Configuration;

public record SubscriptionContext
{
    public required SubjectTypeMapping SubjectMapping { get; init; }
    public required EndpointConfig EndpointConfig { get; init; }
    public required Func<MessageContext, CancellationToken, Task<AckSignal>> Handler { get; init; }

    public static SubscriptionContext ForEndpoint(EndpointConfig endpointConfig,
        Func<MessageContext, CancellationToken, Task<AckSignal>> handler,
        Type? endpointType = null)
    {
        var typedSubject = DefaultSubjectTypeMapper.From(endpointConfig, endpointType);
        return new SubscriptionContext
        {
            EndpointConfig = endpointConfig,
            SubjectMapping = typedSubject,
            Handler = handler,
        };
    }
}

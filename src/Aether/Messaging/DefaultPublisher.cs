using System.Text.Json;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Providers;
using Aether.Messaging.Configuration;

namespace Aether.Messaging;

internal class DefaultPublisher : IPublisher
{
    private readonly IPublisherProvider providerPublisher;
    private readonly PublishConfig publishConfig;
    private readonly DefaultSubjectTypeMapper subjectTypeMapper;

    public DefaultPublisher(PublishConfig publishConfig)
    {
        this.publishConfig = publishConfig ?? throw new ArgumentNullException(nameof(publishConfig));
        providerPublisher = publishConfig.PublisherProvider ?? throw new InvalidOperationException("ProviderPublisher cannot be null.");

        subjectTypeMapper = DefaultSubjectTypeMapper.From(publishConfig);
    }

    public Task Send<TCommand>(TCommand commandMessage, CancellationToken cancellationToken) where TCommand : ICommand
        => PublishInternal(commandMessage, nameof(Send), cancellationToken);

    public Task Broadcast<TEvent>(TEvent eventMessage, CancellationToken cancellationToken) where TEvent : IEvent
        => PublishInternal(eventMessage, nameof(Broadcast), cancellationToken);

    public async Task<TResponse?> Request<TRequest, TResponse>(TRequest requestMessage, CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        var aetherMessage = AetherMessage.From(
            message: requestMessage,
            subjectTypeMapper: subjectTypeMapper,
            action: nameof(Request)
        );
        
        aetherMessage.SetResponseHeaders<TResponse>();

        var response = await providerPublisher.Request(publishConfig, aetherMessage, cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(response);
    }

    private Task PublishInternal<TMessage>(TMessage message, string action, CancellationToken cancellationToken)
    {
        var aetherMessage = AetherMessage.From(
            message: message,
            subjectTypeMapper: subjectTypeMapper,
            action: action
        );
        
        return providerPublisher.Publish(publishConfig, aetherMessage, cancellationToken);
    }
}
static class AetherMessageExtensions
{
    public static void SetResponseHeaders<TResponse>(this AetherMessage message)
    {
        var responseType = typeof(TResponse);
        message.Headers[AetherHeader.ResponseType] = responseType.Name;
        message.Headers[AetherHeader.ResponseClrType] = responseType.AssemblyQualifiedName!;
    }
}

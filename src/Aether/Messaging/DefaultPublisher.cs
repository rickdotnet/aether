using System.Text.Json;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Microsoft.Extensions.Primitives;

namespace Aether.Messaging;

internal class DefaultPublisher : IPublisher
{
    private readonly IPublisherProvider providerPublisher;
    private readonly PublishConfig publishConfig;

    public DefaultPublisher(PublishConfig publishConfig, IPublisherProvider providerPublisher)
    {
        this.publishConfig = publishConfig ?? throw new ArgumentNullException(nameof(publishConfig));
        this.providerPublisher = providerPublisher ?? throw new ArgumentNullException(nameof(providerPublisher)); 
    }

    public Task Send<TCommand>(TCommand commandMessage, CancellationToken cancellationToken) where TCommand : ICommand
        => PublishInternal(commandMessage, nameof(Send), cancellationToken);

    public Task Broadcast<TEvent>(TEvent eventMessage, CancellationToken cancellationToken) where TEvent : IEvent
        => PublishInternal(eventMessage, nameof(Broadcast), cancellationToken);

    public async Task<TResponse?> Request<TRequest, TResponse>(TRequest requestMessage, CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        var subject = DefaultSubjectTypeMapper.From(publishConfig);
        var aetherMessage = CreateMessage(
            message: requestMessage,
            subject,
            action: nameof(Request)
        );
        
        aetherMessage.SetResponseTypeHeaders<TResponse>();

        var response = await providerPublisher.Request(publishConfig, aetherMessage, cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(response);
    }

    private Task PublishInternal<TMessage>(TMessage message, string action, CancellationToken cancellationToken)
    {
        var subject = DefaultSubjectTypeMapper.From(publishConfig);
        var aetherMessage = CreateMessage(
            message,
            subject,
            action
        );
        
        return providerPublisher.Publish(publishConfig, aetherMessage, cancellationToken);
    }
    
    private static AetherMessage CreateMessage<T>(T message, SubjectTypeMapping subjectMapping, string? action)
    {
        var messageType = typeof(T);
        var response = new AetherMessage
        {
            Data = AetherData.Serialize(message),
            MessageType = messageType,
            Headers = new Dictionary<string, StringValues>
            {
                // not setting subject here, subscriber will set it
                [MessageHeader.MessageTypeMapping] = subjectMapping.MappingForType(messageType),
                [MessageHeader.MessageType] = messageType.Name,
                [MessageHeader.MessageClrType] = messageType.AssemblyQualifiedName!,
            },
        };
        
        if (!string.IsNullOrEmpty(action))
            response.Headers[MessageHeader.MessageAction] = action;
        
        return response;
    }
}
static class AetherMessageExtensions
{
    public static void SetResponseTypeHeaders<TResponse>(this AetherMessage message)
    {
        var responseType = typeof(TResponse);
        message.Headers[MessageHeader.ResponseType] = responseType.Name;
        message.Headers[MessageHeader.ResponseClrType] = responseType.AssemblyQualifiedName!;
    }
}

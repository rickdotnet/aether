using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Microsoft.Extensions.Primitives;
using RickDotNet.Base;
using RickDotNet.Extensions.Base;

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

    public Task Send<TType>(AetherData data, CancellationToken cancellationToken = default)
        => PublishInternal<TType>(data, nameof(Send), cancellationToken);

    public Task Broadcast<TEvent>(TEvent eventMessage, CancellationToken cancellationToken) where TEvent : IEvent
        => PublishInternal(eventMessage, nameof(Broadcast), cancellationToken);

    public Task Broadcast<TType>(AetherData data, CancellationToken cancellationToken = default)
        => PublishInternal<TType>(data, nameof(Broadcast), cancellationToken);

    public Task<Result<AetherData>> Request<TRequest>(TRequest requestMessage, CancellationToken cancellationToken)
        => Request<TRequest>(AetherData.Serialize(requestMessage), cancellationToken);

    public Task<Result<AetherData>> Request<TRequest>(AetherData requestData, CancellationToken cancellationToken)
        => RequestInternal<TRequest, AetherData>(requestData, cancellationToken);

    public Task<Result<AetherData>> Request<TRequest>(AetherData<TRequest> requestData, CancellationToken cancellationToken) 
        => RequestInternal<TRequest, AetherData>(requestData.Data, cancellationToken);

    public Task<TResponse?> Request<TRequest, TResponse>(TRequest requestMessage, CancellationToken cancellationToken) where TRequest : IRequest<TResponse> 
        => Request<TRequest, TResponse>(AetherData.Serialize(requestMessage), cancellationToken);

    public async Task<TResponse?> Request<TRequest, TResponse>(AetherData requestData, CancellationToken cancellationToken)
    {
        var result = await RequestInternal<TRequest, TResponse>(requestData, cancellationToken);
        var response = result.Select(data => data.As<TResponse>());
        return response.ValueOrDefault();
    }

    private Task PublishInternal<TMessage>(TMessage message, string action, CancellationToken cancellationToken) where TMessage : IMessage
    {
        var data = AetherData.Serialize(message);
        return PublishInternal<TMessage>(data, action, cancellationToken);
    }

    private Task PublishInternal<TType>(AetherData data, string? action, CancellationToken cancellationToken)
    {
        var subject = DefaultSubjectTypeMapper.From(publishConfig);
        var aetherMessage = CreateMessage(
            data,
            typeof(TType),
            subject,
            action
        );

        return providerPublisher.Publish(publishConfig, aetherMessage, cancellationToken);
    }

    private async Task<Result<AetherData>> RequestInternal<TRequest, TResponse>(AetherData data, CancellationToken cancellationToken)
    {
        var subject = DefaultSubjectTypeMapper.From(publishConfig);
        var aetherMessage = CreateMessage(
            data,
            typeof(TRequest),
            subject,
            action: nameof(Request)
        );
        aetherMessage.SetResponseTypeHeaders<TResponse>();

        return await providerPublisher.Request(publishConfig, aetherMessage, cancellationToken);
    }

    private static AetherMessage CreateMessage(AetherData data, Type dataType, SubjectTypeMapping subjectMapping, string? action)
    {
        var response = new AetherMessage
        {
            Data = data,
            MessageType = dataType,
            Headers = new Dictionary<string, StringValues>
            {
                // not setting subject here, subscriber will set it
                [MessageHeader.MessageTypeMapping] = subjectMapping.MappingForType(dataType),
                [MessageHeader.MessageType] = dataType.Name,
                [MessageHeader.MessageClrType] = dataType.AssemblyQualifiedName!,
            },
        };

        if (!string.IsNullOrEmpty(action))
            response.Headers[MessageHeader.MessageAction] = action.ToLower();

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

using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using RickDotNet.Base;
using RickDotNet.Extensions.Base;

namespace Aether.Providers.NATS.Messaging;

internal class NatsCoreSubscription : ISubscription
{
    private readonly INatsConnection connection;
    private readonly ILogger<NatsCoreSubscription> logger;
    private readonly SubscriptionConfig subConfig;
    private readonly Func<MessageContext, CancellationToken, Task<Result<VoidResult>>> handler;
    private readonly string endpointSubject;
    private readonly DefaultSubjectTypeMapper subjectTypeMapper;

    public NatsCoreSubscription(
        INatsConnection connection,
        ILogger<NatsCoreSubscription> logger,
        SubscriptionContext subscriptionContext
    )
    {
        this.connection = connection;
        this.logger = logger;
        subConfig = subscriptionContext.SubscriptionConfig;
        handler = subscriptionContext.Handler;

        subjectTypeMapper = DefaultSubjectTypeMapper.From(subConfig);
        endpointSubject = subjectTypeMapper.Subject;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Subscribing to {Endpoint} - {Subject}", subConfig.EndpointConfig.EndpointName,
                endpointSubject);
            await foreach (var msg in connection.SubscribeAsync<byte[]>(endpointSubject,
                               cancellationToken: cancellationToken))
            {
                var result = await ProcessMessage(msg);
                result.OnFailure(error =>
                    logger.LogError("Error processing message from {Subject}: {Error}", msg.Subject, error));
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("TaskCanceledException");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error subscribing to {EndpointSubject}", endpointSubject);
        }

        return;

        Task<Result<VoidResult>> ProcessMessage(NatsMsg<byte[]> natsMsg)
        {
            var message = new AetherMessage
            {
                Headers = natsMsg.Headers ?? new NatsHeaders(),
                Data = natsMsg.Data ?? [],
            };

            // if we have a subject mapping header, use it to determine the message type
            if (message.Headers.TryGetValue(MessageHeader.SubjectMapping, out var headerType) && headerType.Count > 0)
            {
                message.MessageType =
                    subjectTypeMapper.TypeFromAetherMessageType(headerType.First()!);
            }

            var replyFunc = natsMsg.ReplyTo != null
                ? new Func<byte[], CancellationToken, Task>(
                    (response, innerCancel) =>
                        connection.PublishAsync(natsMsg.ReplyTo, response, cancellationToken: innerCancel).AsTask()
                )
                : null;

            return handler(new MessageContext(message, replyFunc), cancellationToken);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
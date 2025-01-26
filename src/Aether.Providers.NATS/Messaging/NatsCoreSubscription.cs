using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Aether.Providers.NATS.Messaging;

internal class NatsCoreSubscription : ISubscription
{
    private readonly INatsConnection connection;
    private readonly ILogger<NatsCoreSubscription> logger;
    private readonly SubscriptionConfig subConfig;
    private readonly Func<MessageContext, CancellationToken, Task> handler;
    private readonly string endpointSubject;
    private readonly Dictionary<string, Type> subjectTypeMapping;
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
        subjectTypeMapping = subjectTypeMapper.SubjectTypeMapping;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Subscribing to {Endpoint} - {Subject}", subConfig.EndpointConfig.EndpointName, endpointSubject);
            await foreach (var msg in connection.SubscribeAsync<byte[]>(endpointSubject, cancellationToken: cancellationToken))
            {
                try
                {
                    // type mapping is for endpoint types only
                    var subjectMapping = "";
                    if (msg.Headers != null && msg.Headers.TryGetValue(MessageHeader.SubjectMapping, out var aetherType))
                        subjectMapping = aetherType.First() ?? "";

                    if (subConfig.HandlerOnly || subjectTypeMapping.ContainsKey(subjectMapping))
                        await ProcessMessage(msg);
                    else
                        logger.LogWarning(
                            "No handler found for {Subject} in endpoint ({Endpoint})",
                            msg.Subject,
                            subConfig.EndpointConfig.EndpointName);
                }

                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message from {Subject}", msg.Subject);
                }
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

        Task ProcessMessage(NatsMsg<byte[]> natsMsg)
        {
            var message = new AetherMessage
            {
                Headers = natsMsg.Headers ?? new NatsHeaders(),
                Data = natsMsg.Data ?? [],
            };

            if (message.Headers.TryGetValue(MessageHeader.SubjectMapping, out var headerType) && headerType.Count > 0)
            {
                message.MessageType =
                    subjectTypeMapper.TypeFromAetherMessageType(headerType.First()!); // ?? typeof(byte[]);
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

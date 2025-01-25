using Aether.Abstractions.Messaging;
using Aether.Messaging;
using Aether.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Aether.Providers.NATS.Messaging;

internal class NatsCoreSubscription : ISubscription
{
    private readonly INatsConnection connection;
    private readonly ILogger<NatsCoreSubscription> logger;
    private readonly SubscriptionConfig config;
    private readonly Func<MessageContext, CancellationToken, Task> handler;
    private readonly string endpointSubject;
    private readonly Dictionary<string, Type> subjectTypeMapping;
    private readonly DefaultSubjectTypeMapper subjectTypeMapper;

    public NatsCoreSubscription(
        INatsConnection connection,
        ILogger<NatsCoreSubscription> logger,
        SubscriptionConfig config,
        Func<MessageContext, CancellationToken, Task> handler
    )
    {
        this.connection = connection;
        this.logger = logger;
        this.config = config;
        this.handler = handler;

        subjectTypeMapper = DefaultSubjectTypeMapper.From(config);
        endpointSubject = subjectTypeMapper.Subject;
        subjectTypeMapping = subjectTypeMapper.SubjectTypeMapping;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Subscribing to {Endpoint} - {Subject}", config.EndpointName, endpointSubject);
            await foreach (var msg in connection.SubscribeAsync<byte[]>(endpointSubject)
                               .WithCancellation(cancellationToken))
            {
                var handlerOnly = config.EndpointType == null;
                try
                {
                    // type mapping is for endpoint types only
                    var subjectMapping = "";
                    if (msg.Headers != null && msg.Headers.TryGetValue(MessageHeader.SubjectMapping, out var aetherType))
                        subjectMapping = aetherType.First() ?? "";

                    if (handlerOnly || subjectTypeMapping.ContainsKey(subjectMapping))
                        await ProcessMessage(msg);
                    else
                        logger.LogWarning(
                            "No handler found for {Subject} in endpoint ({Endpoint})",
                            msg.Subject,
                            config.EndpointName);
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

            if (message.Headers.TryGetValue(MessageHeader.SubjectMapping, out var headerType)
                && headerType.Count > 0)
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
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using ConsumerConfig = NATS.Client.JetStream.Models.ConsumerConfig;

namespace Aether.Providers.NATS.Messaging;

internal class NatsJetStreamSubscription : ISubscription
{
    private readonly INatsConnection connection;
    private readonly ILogger<NatsJetStreamSubscription> logger;
    private readonly SubscriptionConfig subConfig;
    private readonly EndpointConfig endpointConfig;
    private readonly ConsumerConfig consumerConfig;
    private readonly Func<MessageContext, CancellationToken, Task> handler;
    private readonly string endpointSubject;
    private readonly Dictionary<string, Type> subjectTypeMapping;

    public NatsJetStreamSubscription(
        INatsConnection connection,
        ILogger<NatsJetStreamSubscription> logger,
        SubscriptionContext subscriptionContext
    )
    {
        this.connection = connection;
        this.logger = logger;
        subConfig = subscriptionContext.SubscriptionConfig;
        endpointConfig = subscriptionContext.SubscriptionConfig.EndpointConfig;
        consumerConfig = subscriptionContext.ConsumerConfig!;
        handler = subscriptionContext.Handler;

        var subjectTypeMapper = DefaultSubjectTypeMapper.From(subConfig);
        endpointSubject = subjectTypeMapper.Subject;
        subjectTypeMapping = subjectTypeMapper.SubjectTypeMapping;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        // TODO: this assumes that the stream is already created
        //            stream creation will be handled later by something else
        try
        {
            var js = new NatsJSContext((NatsConnection)connection);

            var streamNameClean = CleanStreamName(endpointSubject);
            var ackStrategy = endpointConfig.AckStrategy;

            logger.LogTrace("Creating consumer {ConsumerName} for stream {StreamName}", consumerConfig.Name, streamNameClean);
            var consumer = await js.CreateOrUpdateConsumerAsync(streamNameClean, consumerConfig, cancellationToken);

            logger.LogInformation("Subscribing to {Subject}", endpointSubject);
            await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken))
            {
                try
                {
                    var subjectMapping = "";
                    if (msg.Headers != null && msg.Headers.TryGetValue(MessageHeader.SubjectMapping, out var aetherType))
                        subjectMapping = aetherType.First() ?? "";

                    if (subConfig.HandlerOnly || subjectTypeMapping.ContainsKey(subjectMapping))
                    {
                        if (ackStrategy == AckStrategy.AutoAck)
                            await msg.AckAsync(cancellationToken: cancellationToken);

                        await ProcessMessage(msg);

                        if (ackStrategy == AckStrategy.Default)
                            await msg.AckAsync(cancellationToken: cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning(
                            "No handler found for {Subject} in endpoint ({Endpoint})",
                            subjectMapping,
                            endpointConfig.EndpointName);

                        await msg.AckTerminateAsync(cancellationToken: cancellationToken);
                    }
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

        Task ProcessMessage(NatsJSMsg<byte[]> natsMsg)
        {
            var message = new AetherMessage
            {
                Headers = natsMsg.Headers ?? new NatsHeaders(),
                Data = natsMsg.Data ?? [],
            };

            var subjectMapping = "";
            if (natsMsg.Headers != null && natsMsg.Headers.TryGetValue(MessageHeader.SubjectMapping, out var aetherType))
                subjectMapping = aetherType.First() ?? "";

            subjectTypeMapping.TryGetValue(subjectMapping, out var messageType);
            message.MessageType = messageType ?? typeof(byte[]);

            var replyFunc = natsMsg.ReplyTo != null
                ? new Func<byte[], CancellationToken, Task>((response, innerCancel) =>
                    connection.PublishAsync(natsMsg.ReplyTo, response, cancellationToken: innerCancel).AsTask()
                )
                : null;

            return handler(new MessageContext(message, replyFunc), cancellationToken);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    
    private static string CleanStreamName(string streamName)
    {
        return streamName.Replace(".", "_")
            .Replace("*", "")
            .Replace(">", "")
            .TrimEnd('_');
    }
}
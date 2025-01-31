using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using RickDotNet.Base;
using RickDotNet.Extensions.Base;
using ConsumerConfig = NATS.Client.JetStream.Models.ConsumerConfig;

namespace Aether.Providers.NATS.Messaging;

internal class NatsJetStreamSubscription : ISubscription
{
    private readonly INatsConnection connection;
    private readonly ILogger<NatsJetStreamSubscription> logger;
    private readonly ConsumerConfig consumerConfig;
    private readonly Func<MessageContext, CancellationToken, Task<Result<VoidResult>>> handler;
    private readonly DefaultSubjectTypeMapper subjectTypeMapper;
    private readonly string endpointSubject;

    public NatsJetStreamSubscription(
        INatsConnection connection,
        ILogger<NatsJetStreamSubscription> logger,
        SubscriptionContext subscriptionContext)
    {
        this.connection = connection;
        this.logger = logger;
        consumerConfig = subscriptionContext.ConsumerConfig!;
        handler = subscriptionContext.Handler;

        subjectTypeMapper = DefaultSubjectTypeMapper.From(subscriptionContext.SubscriptionConfig);
        endpointSubject = subjectTypeMapper.Subject;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        // TODO: this assumes that the stream is already created
        //            stream creation will be handled later by something else
        try
        {
            var js = new NatsJSContext((NatsConnection)connection);

            var streamNameClean = CleanStreamName(endpointSubject);

            logger.LogTrace("Creating consumer {ConsumerName} for stream {StreamName}", consumerConfig.Name,
                streamNameClean);

            var consumer = await js.CreateOrUpdateConsumerAsync(streamNameClean, consumerConfig, cancellationToken);

            logger.LogInformation("Subscribing to stream {Stream}", streamNameClean);
            await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken))
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

        async Task<Result<VoidResult>> ProcessMessage(NatsJSMsg<byte[]> msg)
        {
            var message = new AetherMessage
            {
                Headers = msg.Headers ?? new NatsHeaders(),
                Data = msg.Data ?? [],
            };

            // if we have a subject mapping header, use it to determine the message type
            if (message.Headers.TryGetValue(MessageHeader.SubjectMapping, out var headerType) && headerType.Count > 0)
            {
                message.MessageType =
                    subjectTypeMapper.TypeFromAetherMessageType(headerType.First()!);
            }

            var replyFunc = msg.ReplyTo != null
                ? new Func<byte[], CancellationToken, Task>((response, innerCancel) =>
                    connection.PublishAsync(msg.ReplyTo, response, cancellationToken: innerCancel).AsTask()
                )
                : null;

            var acked = false;
            var ackFunc = new Func<CancellationToken, Task>(async innerCancel =>
            {
                acked = true;
                await msg.AckAsync(cancellationToken: innerCancel);
            });

            var result = await handler(new MessageContext(message, replyFunc, ackFunc), cancellationToken);
            await result.ResolveAsync(
                onSuccess: async _ =>
                {
                    // temporary until we rework the synchronous endpoint
                    if (!acked)
                        await msg.AckAsync(cancellationToken: cancellationToken);
                },
                onError: async error =>
                {
                    logger.LogError("Error processing message from {Subject}: {Error}", msg.Subject, error);
                    if (!acked)
                        await msg.NakAsync(cancellationToken: cancellationToken);
                });

            return result;
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
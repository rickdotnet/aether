using Aether.Abstractions.Messaging;
using Aether.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
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
    private readonly Func<MessageContext, CancellationToken, Task<AckSignal>> handler;
    private readonly SubjectTypeMapping subjectMapping;

    public NatsJetStreamSubscription(
        INatsConnection connection,
        ILogger<NatsJetStreamSubscription> logger,
        NatsSubscriptionContext subscriptionContext)
    {
        this.connection = connection;
        this.logger = logger;
        consumerConfig = subscriptionContext.ConsumerConfig!;
        handler = subscriptionContext.Handler;
        subjectMapping = subscriptionContext.SubjectMapping;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        // TODO: this assumes that the stream is already created
        //            stream creation will be handled later by something else
        try
        {
            var js = new NatsJSContext((NatsConnection)connection);

            var streamNameClean = CleanStreamName(subjectMapping.Subject);

            logger.LogTrace("Creating consumer {ConsumerName} for stream {StreamName}", consumerConfig.Name,
                streamNameClean);

            var consumer = await js.CreateOrUpdateConsumerAsync(streamNameClean, consumerConfig, cancellationToken);

            var consumeOpts = new NatsJSConsumeOpts
            {
                MaxMsgs = consumerConfig.MaxBatch > 0 ? consumerConfig.MaxBatch : 1000,
            };

            logger.LogInformation("Subscribing to stream {Stream}", streamNameClean);
            await foreach (var msg in consumer.ConsumeAsync<byte[]>(opts: consumeOpts, cancellationToken: cancellationToken))
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
            logger.LogError(ex, "Error subscribing to {EndpointSubject}", subjectMapping);
        }

        return;

        async Task<Result<AckSignal>> ProcessMessage(NatsJSMsg<byte[]> natsMsg)
        {
            var headers = natsMsg.Headers?.ToDictionary(kp => kp.Key, kp => kp.Value) ?? new Dictionary<string, StringValues>();

            headers[MessageHeader.Subject] = natsMsg.Subject;

            if (natsMsg.Subject.StartsWith("$SYS.REQ", StringComparison.OrdinalIgnoreCase))
                headers[MessageHeader.MessageAction] = "request";
            
            var message = new AetherMessage
            {
                Headers = headers,
                Data = natsMsg.Data ?? [],
            };

            // if we have a subject mapping header, use it to determine the message type
            if (message.Headers.TryGetValue(MessageHeader.MessageTypeMapping, out var headerType) &&
                headerType.Count > 0)
            {
                var messageType = subjectMapping.TypeFromMapping(headerType.First()!);
                message.MessageType = messageType;
            }

            var replyFunc = natsMsg.ReplyTo != null
                ? new Func<AetherData, CancellationToken, Task>((response, innerCancel) =>
                    connection.PublishAsync(natsMsg.ReplyTo, response, cancellationToken: innerCancel).AsTask()
                )
                : null;

            var ackFunc =
                new Func<AckSignal, CancellationToken, Task>((signal, innerCancel) =>
                    HandleSignal(signal, natsMsg, innerCancel));

            var result = await Result.TryAsync(
                () => handler(new MessageContext(message, replyFunc, ackFunc), cancellationToken)
            );

            await result.ResolveAsync(
                onSuccess: async signal =>
                {
                    switch (signal)
                    {
                        case AckSignal.Ack:
                            await natsMsg.AckAsync(cancellationToken: cancellationToken);
                            break;
                        case AckSignal.ExplicitAck:
                            // will be acked in endpoint, so need to
                            // hold on to the message until ack function is called
                            break;
                    }
                },
                onError: async error =>
                {
                    logger.LogError("Error processing message from {Subject}: {Error}", natsMsg.Subject, error);
                    await natsMsg.NakAsync(cancellationToken: cancellationToken);
                });

            return result;
        }
    }

    private static Task HandleSignal(AckSignal signal, NatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
    {
        return signal switch
        {
            AckSignal.Ack => msg.AckAsync(cancellationToken: cancellationToken).AsTask(),
            AckSignal.Nak => msg.NakAsync(cancellationToken: cancellationToken).AsTask(),
            _ => Task.CompletedTask
        };
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

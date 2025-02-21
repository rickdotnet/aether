using Aether.Abstractions.Messaging;
using Aether.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;
using RickDotNet.Base;
using RickDotNet.Extensions.Base;

namespace Aether.Providers.NATS.Messaging;

internal class NatsCoreSubscription : ISubscription
{
    private readonly INatsConnection connection;

    private readonly ILogger<NatsCoreSubscription> logger;
    private readonly Func<MessageContext, CancellationToken, Task> handler;
    private readonly SubjectTypeMapping subjectMapping;

    public NatsCoreSubscription(
        INatsConnection connection,
        ILogger<NatsCoreSubscription> logger,
        NatsSubscriptionContext subscriptionContext
    )
    {
        this.connection = connection;
        this.logger = logger;
        handler = subscriptionContext.Handler;
        subjectMapping = subscriptionContext.SubjectMapping;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Subscribing to {Subject}", subjectMapping.Subject);
            await foreach (var msg in connection.SubscribeAsync<byte[]>(subjectMapping.Subject, cancellationToken: cancellationToken))
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

        Task<Result<VoidResult>> ProcessMessage(NatsMsg<byte[]> natsMsg)
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

            // if we have a type mapping header, use it to determine the message type
            if (message.Headers.TryGetValue(MessageHeader.MessageTypeMapping, out var headerType) &&
                headerType.Count > 0)
            {
                var messageType = subjectMapping.TypeFromMapping(headerType.First()!);
                message.MessageType = messageType;
            }

            var replySubject = natsMsg.ReplyTo;
            var replyFunc = replySubject != null
                ? new Func<AetherData, CancellationToken, Task>(
                    (response, innerCancel) =>
                        connection.PublishAsync(replySubject, response.Data, cancellationToken: innerCancel).AsTask()
                )
                : null;

            return Result.TryAsync(() => handler(new MessageContext(message, replyFunc), cancellationToken));
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

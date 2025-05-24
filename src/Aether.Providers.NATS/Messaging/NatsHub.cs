using Aether.Abstractions.Messaging;
using Aether.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;
using RickDotNet.Base;
using RickDotNet.Extensions.Base;

namespace Aether.Providers.NATS.Messaging;

public sealed class NatsHub : IMessageHub
{
    private readonly INatsConnection connection;

    private readonly ILogger<NatsHub> logger;

    public NatsHub(
        INatsConnection connection,
        ILogger<NatsHub> logger
    )
    {
        this.connection = connection;
        this.logger = logger;
    }

    public void AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            logger.LogInformation("Subscribing to {Subject}", endpointConfig.FullSubject);
            await foreach (var msg in connection.SubscribeAsync<byte[]>(endpointConfig.FullSubject, cancellationToken: cancellationToken))
            {
                try
                {
                    Console.WriteLine("Received message");
                    var result = await InnerHandle(msg, handler, cancellationToken);
                    result.OnError(error => logger.LogError("{Error}", error));
                }
                catch (TaskCanceledException)
                {
                    logger.LogWarning("TaskCanceledException");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error subscribing to {EndpointSubject}", endpointConfig.FullSubject);
                }
            }
        }, cancellationToken);
    }

    private Task<Result<VoidResult>> InnerHandle(
        NatsMsg<byte[]> natsMsg,
        Func<MessageContext, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        var headers = natsMsg.Headers?.ToDictionary(kp => kp.Key, kp => kp.Value) ?? new Dictionary<string, StringValues>();
        headers[MessageHeader.Subject] = natsMsg.Subject;

        // temp, and not sure that it's required anymore
        // this was legacy stuff
        if (natsMsg.Subject.StartsWith("$SYS.REQ", StringComparison.OrdinalIgnoreCase))
            headers[MessageHeader.MessageAction] = "request";

        var message = new AetherMessage
        {
            Headers = headers,
            Data = natsMsg.Data ?? [],
        };

        var replySubject = natsMsg.ReplyTo;
        var replyFunc = replySubject != null
            ? new Func<AetherData, CancellationToken, Task>((response, innerCancel) =>
                connection.PublishAsync(replySubject, response.Data, cancellationToken: innerCancel).AsTask()
            )
            : null;

        return Result.TryAsync(() => handler(new MessageContext(message, replyFunc), cancellationToken));
    }

    public async Task<Result<VoidResult>> Send(AetherMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Subject == null)
            return Result.Failure<VoidResult>("No subject specified");

        var natsHeaders = new NatsHeaders(message.Headers);
        await connection.PublishAsync<Memory<byte>>(message.Subject!, message.Data, headers: natsHeaders, cancellationToken: cancellationToken);
        return Result.Success();
    }

    public async Task<Result<AetherData>> Request(AetherMessage message, CancellationToken cancellationToken)
    {
        if (message.Subject == null)
            return Result.Failure<AetherData>("No subject specified");

        var natsHeaders = new NatsHeaders(message.Headers);
        var result = await connection.RequestAsync<Memory<byte>,Memory<byte>>(
            subject: message.Subject!,
            data: message.Data,
            headers: natsHeaders, 
            cancellationToken: cancellationToken);
        return Result.Success(new AetherData(result.Data));
    }

    public ValueTask DisposeAsync()
        => connection.DisposeAsync();
}

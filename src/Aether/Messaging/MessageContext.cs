using Aether.Abstractions.Messaging;
using Microsoft.Extensions.Primitives;
using RickDotNet.Base;

namespace Aether.Messaging;

public class MessageContext
{
    public IReadOnlyDictionary<string, StringValues> Headers { get; }
    public AetherData Data => Message.Data ?? AetherData.Empty;
    internal AetherMessage Message { get; }
    public bool ReplyAvailable => ReplyFunc is not null;
    public bool AckAvailable => AckFunc is not null;
    private Func<byte[], CancellationToken, Task>? ReplyFunc { get; }
    public Func<CancellationToken, Task>? AckFunc { get; }

    public MessageContext(AetherMessage message, Func<byte[], CancellationToken, Task>? replyFunc = null,
        Func<CancellationToken, Task>? ackFunc = null)
    {
        Message = message;
        ReplyFunc = replyFunc;
        AckFunc = ackFunc;

        Headers = Message.Headers.AsReadOnly();
    }

    public Task<Result<VoidResult>> Reply(byte[] response, CancellationToken cancellationToken)
        => Task.FromResult(
            ReplyAvailable
                ? Result.Failure<VoidResult>("No reply function available")
                : Result.Try(() => { ReplyFunc(response, cancellationToken); })
        );

    public Task<Result<VoidResult>> Ack(CancellationToken cancellationToken)
        => Task.FromResult(
            AckAvailable
                ? Result.Failure<VoidResult>("No ack function available")
                : Result.Try(() => { AckFunc!(cancellationToken); })
        );
}
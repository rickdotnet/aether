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
    public bool SignalAvailable => SignalFunc is not null;
    private Func<byte[], CancellationToken, Task>? ReplyFunc { get; }
    public Func<AckSignal, CancellationToken, Task>? SignalFunc { get; }

    public MessageContext(AetherMessage message, Func<byte[], CancellationToken, Task>? replyFunc = null,
        Func<AckSignal, CancellationToken, Task>? signalFunc = null)
    {
        Message = message;
        ReplyFunc = replyFunc;
        SignalFunc = signalFunc;

        Headers = Message.Headers.AsReadOnly();
    }

    public Task<Result<VoidResult>> Reply(byte[] response, CancellationToken cancellationToken)
        => Task.FromResult(
            ReplyAvailable
                ? Result.Failure<VoidResult>("No reply function available")
                : Result.Try(() => { ReplyFunc!(response, cancellationToken); })
        );

    public Task<Result<VoidResult>> Signal(AckSignal signal, CancellationToken cancellationToken)
        => Task.FromResult(
            SignalAvailable
                ? Result.Failure<VoidResult>("No ack function available")
                : Result.Try(() => { SignalFunc!(signal, cancellationToken); })
        );
}
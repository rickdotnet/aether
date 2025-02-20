using Aether.Abstractions.Messaging;
using Microsoft.Extensions.Primitives;
using RickDotNet.Base;

namespace Aether.Messaging;

public class MessageContext
{
    public IReadOnlyDictionary<string, StringValues> Headers { get; }
    public AetherData Data => Message.Data ?? AetherData.Empty;
    internal AetherMessage Message { get; }
    
    private bool replyCalled;
    public bool ReplyAvailable => !replyCalled && ReplyFunc is not null;

    public bool SignalAvailable => SignalFunc is not null;
    private Func<AetherData, CancellationToken, Task>? ReplyFunc { get; }
    public Func<AckSignal, CancellationToken, Task>? SignalFunc { get; }

    public MessageContext(AetherMessage message, Func<AetherData, CancellationToken, Task>? replyFunc = null,
        Func<AckSignal, CancellationToken, Task>? signalFunc = null)
    {
        Message = message;
        ReplyFunc = replyFunc;
        SignalFunc = signalFunc;

        Headers = Message.Headers.AsReadOnly();
    }

    public Task<Result<VoidResult>> Reply(AetherData response, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            ReplyAvailable
                ? Result.Try(() =>
                {
                    replyCalled = false;
                    ReplyFunc!(response, cancellationToken);
                })
                : Result.Failure<VoidResult>("No reply function available")
        );
    }

    public Task<Result<VoidResult>> Signal(AckSignal signal, CancellationToken cancellationToken)
        => Task.FromResult(
            SignalAvailable
                ? Result.Try(() => { SignalFunc!(signal, cancellationToken); })
                : Result.Failure<VoidResult>("No ack function available")
        );
}
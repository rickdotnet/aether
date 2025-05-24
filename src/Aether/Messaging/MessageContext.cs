using Aether.Abstractions.Messaging;
using Microsoft.Extensions.Primitives;
using RickDotNet.Base;

namespace Aether.Messaging;

public class MessageContext
{
    public IReadOnlyDictionary<string, StringValues> Headers { get; }
    public string? Subject => Message.Subject;
    public AetherData Data => Message.Data ?? AetherData.Empty;
    internal AetherMessage Message { get; }

    private bool replyCalled;
    public bool ReplyAvailable => !replyCalled && Message.IsRequest;

    private Func<AetherData, CancellationToken, Task>? ReplyFunc { get; }

    public MessageContext(AetherMessage message, Func<AetherData, CancellationToken, Task>? replyFunc = null,
        Func<CancellationToken, Task>? signalFunc = null)
    {
        Message = message;
        ReplyFunc = replyFunc;

        Headers = Message.Headers.AsReadOnly();
    }

    public Task<Result<VoidResult>> Reply(AetherData response, CancellationToken cancellationToken)
    {
        if (!ReplyAvailable)
            return Task.FromResult(Result.Failure<VoidResult>("Reply function not available"));

        replyCalled = true;
        return Task.FromResult(Result.Try(() => { ReplyFunc!(response, cancellationToken); }));

    }
}

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
    private Func<byte[], CancellationToken, Task>? ReplyFunc { get; }

    public MessageContext(AetherMessage message, Func<byte[], CancellationToken, Task>? replyFunc = null)
    {
        Message = message;
        ReplyFunc = replyFunc;
        Headers = Message.Headers.AsReadOnly();
    }

    public Task<Result<VoidResult>> Reply(byte[] response, CancellationToken cancellationToken)
        => Task.FromResult(
            ReplyFunc is null
                ? Result.Failure<VoidResult>("No reply function available")
                : Result.Try(() => { ReplyFunc(response, cancellationToken); })
        );
}
using Aether.Abstractions.Messaging;
using Microsoft.Extensions.Primitives;

namespace Aether.Messaging;

public class MessageContext
{
    public IReadOnlyDictionary<string, StringValues> Headers { get; }
    public AetherData Data => Message.Data ?? AetherData.Empty;
    internal AetherMessage Message { get; }
    internal bool ReplyAvailable => ReplyFunc is not null;
    private Func<byte[], CancellationToken, Task>? ReplyFunc { get; }

    public MessageContext(AetherMessage message, Func<byte[], CancellationToken, Task>? replyFunc = null)
    {
        Message = message;
        ReplyFunc = replyFunc;
        Headers = Message.Headers.AsReadOnly();
    }

    internal Task Reply(byte[] response, CancellationToken cancellationToken)
    {
        if (ReplyFunc is null)
            throw new InvalidOperationException("Reply function is not available");

        return ReplyFunc(response, cancellationToken);
    }
}
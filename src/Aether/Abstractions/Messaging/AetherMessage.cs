using Microsoft.Extensions.Primitives;

namespace Aether.Abstractions.Messaging;

public sealed record AetherMessage
{
    public bool IsRequest =>
        Headers.ContainsKey(MessageHeader.MessageAction) && Headers[MessageHeader.MessageAction].Equals("request");

    public Dictionary<string, StringValues> Headers { get; set; } = new();
    public string? Subject => Headers[MessageHeader.Subject];
    public Type? MessageType { get; set; }
    public AetherData Data { get; init; } = null!;

    public override string ToString() => "Aether Message!!";

    public static AetherMessage For<T>(string subject, T payload)
    {
        var action = payload switch
        {
            ICommand _ => "command",
            IEvent _ => "event",
            IRequest _ => "request",
            _ => "unknown"
        };
        return For(subject, action, AetherData<T>.Serialize(payload));
    }

    private static AetherMessage For<T>(string subject, string action, AetherData<T> data, Dictionary<string, StringValues>? headers = null)
    {
        var message = new AetherMessage
        {
            MessageType = data.Type,
            Data = data.Data,
            Headers = new Dictionary<string, StringValues>
            {
                [MessageHeader.Subject] = subject,
                [MessageHeader.MessageAction] = action,
            }
        };

        if (headers != null)
        {
            foreach (var header in headers)
            {
                message.Headers[header.Key] = header.Value;
            }
        }

        return message;
    }
}

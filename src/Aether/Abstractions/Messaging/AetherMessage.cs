using Microsoft.Extensions.Primitives;

namespace Aether.Abstractions.Messaging;

public sealed record AetherMessage
{
    public bool IsRequest => 
        Headers.ContainsKey(MessageHeader.MessageAction) 
        && Headers[MessageHeader.MessageAction].Equals("request");
    public IDictionary<string, StringValues> Headers { get; set; } = new Dictionary<string, StringValues>();
    public string? Subject => Headers[MessageHeader.Subject];
    public Type? MessageType { get; set; }
    public AetherData? Data { get; set; }
    public override string ToString() => "Aether Message!!";
}
using Microsoft.Extensions.Primitives;

namespace Aether.Abstractions.Messaging;

public sealed record AetherMessage
{
    public IDictionary<string, StringValues> Headers { get; set; } = new Dictionary<string, StringValues>();
    public string? Subject => Headers[MessageHeader.Subject];
    public Type? MessageType { get; set; }
    public AetherData? Data { get; set; }
    public override string ToString() => "Aether Message!!";

    internal static AetherMessage From<T>(T message, SubjectTypeMapping subjectMapping, string? action)
    {
        var messageType = typeof(T);
        var response = new AetherMessage
        {
            Data = AetherData.From(message),
            MessageType = messageType,
            Headers = new Dictionary<string, StringValues>
            {
                [MessageHeader.Subject] = subjectMapping.Subject,
                [MessageHeader.SubjectMapping] = subjectMapping.SubjectMappingForType(messageType),
                [MessageHeader.MessageType] = messageType.Name,
                [MessageHeader.MessageClrType] = messageType.AssemblyQualifiedName!,
            },
        };
        
        if (!string.IsNullOrEmpty(action))
            response.Headers[MessageHeader.MessageAction] = action;
        
        return response;
    }
}
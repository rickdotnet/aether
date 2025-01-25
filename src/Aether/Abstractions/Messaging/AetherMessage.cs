using Microsoft.Extensions.Primitives;

namespace Aether.Abstractions.Messaging;

public sealed record AetherMessage
{
    public IDictionary<string, StringValues> Headers { get; set; } = new Dictionary<string, StringValues>();
    public Type? MessageType { get; set; }
    public AetherData? Data { get; set; }
    public override string ToString() => "Aether Message!!";

    internal static AetherMessage From<T>(T message, ISubjectTypeMapper subjectTypeMapper, string? action)
    {
        var messageType = typeof(T);
        var response = new AetherMessage
        {
            Data = AetherData.From(message),
            MessageType = messageType,
            Headers = new Dictionary<string, StringValues>
            {
                [AetherHeader.Subject] = subjectTypeMapper.Subject,
                [AetherHeader.SubjectMapping] = subjectTypeMapper.TypeMappedSubject(messageType),
                [AetherHeader.MessageType] = messageType.Name,
                [AetherHeader.MessageClrType] = messageType.AssemblyQualifiedName!,
            },
        };
        
        if (!string.IsNullOrEmpty(action))
            response.Headers[AetherHeader.MessageAction] = action;
        
        return response;
    }
}
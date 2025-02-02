namespace Aether.Abstractions.Messaging;

/// <summary>
/// Legacy bridge that supports the Apollo subject + message type mappings. This is on the chopping block.
/// </summary>
public record SubjectTypeMapping
{
    public required string Subject { get; init; }

    public IReadOnlyDictionary<string, Type> TypeMapping { get; init; } = new Dictionary<string, Type>();

    public Type? TypeFromMapping(string headerMessageType) => TypeMapping.GetValueOrDefault(headerMessageType);
    public string MappingForType(Type messageType)
        => DefaultSubjectTypeMapper.MessageTypeMapping(Subject, messageType);
}
using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;

namespace Aether.Abstractions.Messaging;

public record SubjectTypeMapping
{
    public required string Subject { get; init; }

    public IReadOnlyDictionary<string, Type> TypeMapping { get; init; } = new Dictionary<string, Type>();

    public Type? TypeFromMapping(string headerMessageType) => TypeMapping.GetValueOrDefault(headerMessageType);
    public string SubjectMappingForType(Type messageType)
        => DefaultSubjectTypeMapper.TypeMappedSubject(Subject, messageType);
}

public class DefaultSubjectTypeMapper
{
    public static SubjectTypeMapping From(EndpointConfig endpointConfig, Type? type = null)
    {
        var subject = GetSubject(endpointConfig);
        var messageTypes = type?.GetHandlerTypes();
        return new SubjectTypeMapping
        {
            Subject = subject,
            TypeMapping =
                messageTypes?.ToDictionary(x => TypeMappedSubject(subject, x), x => x) ?? []
        };
    }
    
    public static SubjectTypeMapping From(PublishConfig publishConfig) 
        => new() { Subject = GetSubject(publishConfig) };

    public static string TypeMappedSubject(string subject, Type messageType) =>
        $"{subject}.{messageType.Name.ToLower()}";

    private static string GetSubject(PublishConfig config)
        => GetSubject((config.Namespace, config.EndpointName, config.Subject));

    private static string GetSubject(EndpointConfig endpointConfig)
        => GetSubject((endpointConfig.Namespace, endpointConfig.EndpointName,  endpointConfig.Subject));

    private static string GetSubject(
        (string? Namespace, string? EndpointName, string? EndpointSubject) config)
    {
        if (string.IsNullOrEmpty(config.Namespace)
            && string.IsNullOrEmpty(config.EndpointName)
            && string.IsNullOrEmpty(config.EndpointSubject))
        {
            throw new ArgumentException("Namespace, EndpointName, or EndpointSubject must be set");
        }

        // if subject is explicitly set, use it
        var endpoint = config.EndpointSubject;

        // if subject is not set, try to determine it from the endpoint name
        if (string.IsNullOrEmpty(endpoint))
            endpoint = Slugify(config.EndpointName);

        // if namespace is set, prepend it to the endpoint
        if (!string.IsNullOrWhiteSpace(config.Namespace))
        {
            endpoint = !string.IsNullOrWhiteSpace(endpoint)
                ? $"{config.Namespace}.{endpoint}"
                : config.Namespace;
        }

        // if we still don't have a subject, throw an exception
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint could not be determined");

        // temp fix for NATS case sensitivity
        return endpoint.StartsWith('$')
            ? endpoint.ToUpper()
            : endpoint.ToLower();
    }

    private static string? Slugify(string? input) => input?.ToLower().Replace(" ", "-");
}
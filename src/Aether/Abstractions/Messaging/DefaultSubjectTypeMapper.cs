using Aether.Abstractions.Messaging.Configuration;

namespace Aether.Abstractions.Messaging;

public interface ISubjectTypeMapper
{
    string Subject { get; }

    string TypeMappedSubject(Type messageType);
    Dictionary<string, Type> SubjectTypeMapping { get; }
}

public class DefaultSubjectTypeMapper : ISubjectTypeMapper
{
    
    public required string Subject { get; init; }

    public string TypeMappedSubject(Type messageType) => TypeMappedSubject(Subject, messageType);

    public Dictionary<string, Type> SubjectTypeMapping { get; init; } = new();

    public static DefaultSubjectTypeMapper From(SubscriptionConfig subscriptionConfig)
    {
        var subject = GetSubject(subscriptionConfig);
        return new DefaultSubjectTypeMapper()
        {
            Subject = subject,
            SubjectTypeMapping =
                subscriptionConfig.MessageTypes.ToDictionary(x => TypeMappedSubject(subject, x), x => x)
        };
    }
    
    public static DefaultSubjectTypeMapper From(PublishConfig publishConfig) => new() { Subject = GetSubject(publishConfig) };

    // in other versions, the subject includes the message type
    // might revisit this later
    public string MessageTypeSubject(string messageType) => $"{Subject}.{messageType.ToLower()}";
    
    public Type? TypeFromAetherMessageType(string headerMessageType) 
        => SubjectTypeMapping.GetValueOrDefault(headerMessageType);


    public static string TypeMappedSubject(string subject, Type messageType) =>
        $"{subject}.{messageType.Name.ToLower()}";

    private static string GetSubject(PublishConfig config)
        => GetSubject((config.Namespace, config.EndpointName, EndpointType: null, config.Subject));

    private static string GetSubject(SubscriptionConfig subConfig)
        => GetSubject((subConfig.EndpointConfig.Namespace, subConfig.EndpointConfig.EndpointName, subConfig.EndpointType, subConfig.EndpointConfig.Subject));

    private static string GetSubject(
        (string? Namespace, string? EndpointName, Type? EndpointType, string? EndpointSubject) config)
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

        // if endpoint name is not set, try to determine it from the endpoint type
        if (string.IsNullOrEmpty(endpoint))
            endpoint = Slugify(config.EndpointType?.Name);

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
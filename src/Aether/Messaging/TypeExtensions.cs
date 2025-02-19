using Aether.Abstractions.Messaging;

namespace Aether.Messaging;

internal static class TypeExtensions
{
    private static readonly Type[] SupportedInterfaces = {
        typeof(IListenFor<>),
        typeof(IHandle<>),
        typeof(IReplyTo<,>)
    };

    public static Type[] GetHandlerTypes(this Type endpointType)
    {
        var interfaces = endpointType.GetInterfaces();
        return interfaces.Where(i => i.IsGenericType && 
                                     SupportedInterfaces.Contains(i.GetGenericTypeDefinition()))
            .Select(i => i.GetGenericArguments()[0])
            .ToArray();
    }
}
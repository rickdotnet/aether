using Aether.Abstractions.Messaging;
using Aether.Messaging;
using RickDotNet.Base;

namespace Aether.Extensions.Microsoft.Hosting.Messaging;

public sealed class HubRegistration
{
    private readonly List<EndpointRegistration> endpointRegistrations = new();
    public IReadOnlyList<EndpointRegistration> EndpointRegistrations => endpointRegistrations;
    public string HubName { get; }
    public Type HubType { get; private set; }

    public HubRegistration(string hubName, Type hubType)
    {
        HubName = hubName;
        HubType = hubType;
    }

    public void AddRegistration<T>(EndpointConfig config)
        => endpointRegistrations.Add(EndpointRegistration.From<T>(config));

    public void AddRegistration(Type endpointType, EndpointConfig endpointConfig)
        => endpointRegistrations.Add(EndpointRegistration.From(endpointConfig, endpointType));

    public void AddRegistration(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
        => endpointRegistrations.Add(EndpointRegistration.From(endpointConfig, handler));
}

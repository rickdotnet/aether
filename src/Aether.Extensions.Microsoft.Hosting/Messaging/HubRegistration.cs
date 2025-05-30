using Aether.Abstractions.Messaging;
using Aether.Messaging;

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

    public void AddRegistration(EndpointConfig endpointConfig, Type endpointType)
        => endpointRegistrations.Add(EndpointRegistration.From(endpointConfig, endpointType));

    public void AddRegistration(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
        => endpointRegistrations.Add(EndpointRegistration.From(endpointConfig, handler));
}

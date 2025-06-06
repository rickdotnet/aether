namespace Aether.Extensions.Microsoft.Hosting.Messaging;

public sealed class HubRegistration
{
    private readonly List<EndpointRegistration> endpointRegistrations = [];
    public IReadOnlyList<EndpointRegistration> EndpointRegistrations => endpointRegistrations;
    public string HubName { get; }
    public Type HubType { get; private set; }
    
    public HubRegistration(string hubName, Type hubType)
    {
        HubName = hubName;
        HubType = hubType;
    }

    public void AddRegistration(EndpointRegistration registration)
    {
        if (!registration.Validate())
            throw new ArgumentException("Invalid endpoint registration", nameof(registration));

        endpointRegistrations.Add(registration);
    }
}

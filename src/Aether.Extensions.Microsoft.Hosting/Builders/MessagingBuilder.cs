using Aether.Abstractions.Hosting;
using Aether.Abstractions.Messaging;
using Aether.Extensions.Microsoft.Hosting.Messaging;
using Aether.Providers.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Extensions.Microsoft.Hosting.Builders;

internal class MessagingBuilder : IMessagingBuilder
{
    public Type DefaultHubType { get; private set; } = typeof(MemoryHub);
    public List<HubRegistration> HubRegistrations { get; } = new();

    private readonly AetherBuilder aetherBuilder;
    private readonly HashSet<string> hubNames = new();
    

    public MessagingBuilder(AetherBuilder aetherBuilder)
    {
        this.aetherBuilder = aetherBuilder;
    }

    public IMessagingBuilder AddHub(Action<IHubBuilder> configure) 
        => AddHub<MemoryHub>(IDefaultMessageHub.DefaultHubKey, configure);

    public IMessagingBuilder AddHub<T>(string hubName, Action<IHubBuilder> configure) where T : IMessageHub
    {
        if (!hubNames.Add(hubName))
            throw new InvalidOperationException($"Hub with name {hubName} is already registered.");

        var hubType = typeof(T);
        var hubBuilder = new HubBuilder(hubName, hubType, aetherBuilder);
        configure(hubBuilder);

        if (hubName.Equals(IDefaultMessageHub.DefaultHubKey))
            DefaultHubType = hubType;
        
        HubRegistrations.Add(hubBuilder.HubRegistration);

        return this;
    }

    public void RegisterServices(Action<IServiceCollection> configureServices)
    {
        aetherBuilder.RegisterServices(configureServices);
    }
}

using Aether.Abstractions.Hosting;
using Aether.Abstractions.Providers;

namespace Aether.Extensions.Microsoft.Hosting.Builders;

internal class MessagingBuilder : IMessagingBuilder
{
    private readonly AetherBuilder aetherBuilder;
    public MessagingBuilder(AetherBuilder aetherBuilder)
    {
        this.aetherBuilder = aetherBuilder;
    }
    public IMessagingBuilder AddHub(Action<IHubBuilder> configure)
        => AddHub(IDefaultMessageHub.DefaultHubKey, configure);

    public IMessagingBuilder AddHub(string hubName, Action<IHubBuilder> configure)
    {
        var hubBuilder = new HubBuilder(aetherBuilder, hubName);
        configure(hubBuilder);
        
        hubBuilder.Build();
        return this;
    }
}

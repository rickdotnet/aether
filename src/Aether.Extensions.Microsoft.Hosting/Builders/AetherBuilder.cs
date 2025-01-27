using Aether.Abstractions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Extensions.Microsoft.Hosting.Builders;

public class AetherBuilder : IAetherBuilder
{
    private readonly IServiceCollection services;
    private readonly ServiceCollection internalServices = [];

    public IMessagingBuilder Messaging { get; }
    public IStorageBuilder Storage { get; }

    public AetherBuilder(IServiceCollection services)
    {
        this.services = services;
        Messaging = new MessagingBuilder(this);
        Storage = new StorageBuilder();
    }

    internal void Build()
    {
        // register our services with the provided IServiceCollection
        // the reason we're not directly adding the services to the provided IServiceCollection is
        //   1) to allow for additional services to be added by the consumer without affecting the hosted services
        //   2) to provide control over which services are added and in what order
        // for now, we're just adding all the services to the provided IServiceCollection
        foreach (var service in internalServices)
            services.Add(service);

        // we bootstrap a memory client, then swap the
        // implementations at runtime
        services.AddSingleton<AetherClient>(AetherClient.MemoryClient);
        services.AddSingleton<IAetherClient>(p => p.GetRequiredService<AetherClient>());
        services.AddHostedService<HubBackgroundService>();
    }


    public void RegisterServices(Action<IServiceCollection> registerAction)
        => registerAction(internalServices);
}

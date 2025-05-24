using Aether.Abstractions.Hosting;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Storage;
using Aether.Extensions.Microsoft.Hosting.Messaging;
using Aether.Messaging;
using Aether.Providers.Memory;
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
        Storage = new StorageBuilder(this);

        services.AddSingleton<MemoryHub>();
        services.AddSingleton<MemoryStore>();
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

        services.AddSingleton<AetherClient>(p =>
        {
            var defaultHub = (IMessageHub)p.GetRequiredService(Messaging.DefaultHubType);
            var storage = (IStore)p.GetRequiredService(Storage.DefaultStoreType);

            var defaultAetherHub = new AetherHub(defaultHub);
            var client = new AetherClient(defaultAetherHub, storage);
            
            // foreach registration, set the hubs
            var hubRegistrations = ((MessagingBuilder)Messaging).HubRegistrations;
            foreach(var registration in hubRegistrations)
            {
                if (registration.HubName == IDefaultMessageHub.DefaultHubKey)
                {
                    RegisterHandlers(defaultAetherHub, registration.EndpointRegistrations);
                }
                else
                {
                    var hubType = registration.HubType;
                    var hub = (IMessageHub)p.GetRequiredService(hubType);
                    var aetherHub = new AetherHub(hub);
                    client.Messaging.SetHub(registration.HubName, aetherHub);
                    
                    RegisterHandlers(aetherHub, registration.EndpointRegistrations, p);
                }
            }
            
            return client;
        });
        
        services.AddSingleton<IAetherClient>(p => p.GetRequiredService<AetherClient>());
        //services.AddHostedService<AetherBackgroundService>();
    }
    
    private static void RegisterHandlers(AetherHub hub, IReadOnlyList<EndpointRegistration> registrations, IServiceProvider? provider = null)
    {
        foreach(var registration in registrations)
        {
            if (registration.IsHandler)
            {
                var handler = new AetherHandler(registration.Handler!);
                hub.AddHandler(
                    registration.Config,
                    handler.Handle,
                    CancellationToken.None
                );
            }
            else
            {
                var endpointProvider = new DefaultEndpointProvider(provider!);
                var endpointHandler = new AetherHandler(registration.EndpointType!, endpointProvider);
                hub.AddHandler(
                    registration.Config,
                    endpointHandler.Handle,
                    CancellationToken.None
                );
            }
        }
    }


    public void RegisterServices(Action<IServiceCollection> registerAction)
        => registerAction(internalServices);
}

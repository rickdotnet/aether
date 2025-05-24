using Aether.Abstractions.Hosting;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Storage;
using Aether.Providers.NATS.Messaging;
using Aether.Providers.NATS.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;
using NATS.Net;

namespace Aether.Providers.NATS;

public static class Startup
{
    /// <summary>
    /// Adds a NATS hub to the messaging system.
    /// </summary>
    /// <remarks>Assumes NATS Is already configured in the service collection.</remarks>
    public static IMessagingBuilder AddNatsHub(this IMessagingBuilder messaging, Action<IHubBuilder> configure)
        => AddNatsHub(messaging, IDefaultMessageHub.DefaultHubKey, configure);

    /// <summary>
    /// Adds a NATS hub to the messaging system.
    /// </summary>
    /// <remarks>Assumes NATS Is already configured in the service collection.</remarks>
    public static IMessagingBuilder AddNatsHub(this IMessagingBuilder messaging, string hubName, Action<IHubBuilder> configure)
    {
        messaging.RegisterServices(services =>
        {
            services.TryAddSingleton<NatsHub>();
            services.AddSingleton<IMessageHub>(sp => sp.GetRequiredService<NatsHub>());
        });

        return messaging.AddHub<NatsHub>(hubName, configure);
    }

    /// <summary>
    /// Adds a NATS store to the storage system.
    /// </summary>
    /// <remarks>Assumes NATS Is already configured in the service collection.</remarks>
    public static IStorageBuilder AddNatsStore(this IStorageBuilder storageBuilder)
        => AddNatsStore(storageBuilder, IDefaultStore.DefaultStoreName);

    /// <summary>
    /// Adds a NATS store to the storage system.
    /// </summary>
    /// <remarks>Assumes NATS Is already configured in the service collection. Only supports one KV store for now.</remarks>
    public static IStorageBuilder AddNatsStore(this IStorageBuilder storageBuilder, string storeName, int maxBytes = 0)
    {
        storageBuilder.RegisterServices(services =>
        {
            // we're only supporting one KV store for now
            // will move to keyed services later
            services.TryAddSingleton<NatsKvStore>(sp=>
            {
                var kvContext = sp.GetRequiredService<INatsConnection>().CreateKeyValueStoreContext();
                
                // anyone staring at this should be aware,
                // this is a local class, not to be confused
                // with NatsKVStore from the NATS.Client library
                return new NatsKvStore(storeName, kvContext);
            });
        });
        
        storageBuilder.AddStore<NatsKvStore>(storeName, maxBytes);

        return storageBuilder;
    }
}

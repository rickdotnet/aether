using Aether.Abstractions.Hosting;
using Aether.Abstractions.Storage;
using Aether.Providers.NATS.Messaging;
using Aether.Providers.NATS.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Providers.NATS;

public static class Startup
{
    /// <summary>
    /// Assumes NATS Is already configured in the service collection.
    /// </summary>
    public static IHubBuilder UseNats(this IHubBuilder hubBuilder)
    {
        hubBuilder.RegisterServices<NatsSubscriptionProvider, NatsPublisher>(
            services =>
                services
                    .AddSingleton<NatsSubscriptionProvider>()
                    .AddSingleton<NatsPublisher>()
        );

        return hubBuilder;
    }

    public static IStorageBuilder AddNatsStore(this IStorageBuilder storageBuilder)
        => AddNatsStore(storageBuilder, IDefaultStore.DefaultStoreName);

    public static IStorageBuilder AddNatsStore(this IStorageBuilder storageBuilder, string storeName)
    {
        storageBuilder.AddStore(new StorageRegistration(storeName, typeof(NatsStoreProvider)));
        return storageBuilder;
    }
    
    public static IStorageBuilder AddNatsStore(this IStorageBuilder storageBuilder, string storeName, int maxBytes)
    {
        storageBuilder.AddStore(new StorageRegistration(storeName, typeof(NatsStoreProvider))
        {
            MaxBytes = maxBytes
        });
        
        return storageBuilder;
    }
}
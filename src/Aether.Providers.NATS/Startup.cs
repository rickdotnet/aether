using Aether.Abstractions.Hosting;
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

    public static IStorageBuilder AddNatsStore(this IStorageBuilder storageBuilder, string storeName)
    {
        storageBuilder.AddStore<NatsStorageProviderFactory>(storeName);
        return storageBuilder;
    }
}
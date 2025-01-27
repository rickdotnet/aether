using Aether.Abstractions.Hosting;
using Aether.Providers.NATS.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Providers.NATS;

public static class Setup
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
}

using Aether.Abstractions.Hosting;
using Aether.Providers.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Extensions.Microsoft.Hosting;

public static class HubBuilderExtensions
{
    public static IHubBuilder UseMemory(this IHubBuilder hubBuilder)
    {
        hubBuilder.RegisterServices<InMemoryMessageHubProvider, InMemoryMessageHubProvider>(
            services =>
                services
                    .AddSingleton<InMemoryMessageHubProvider>()
        );

        return hubBuilder;
    }
}
using Aether.Abstractions.Hosting;
using Aether.Abstractions.Messaging;
using Aether.Messaging;
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

    public static IAetherEndpoint CreateEndpoint(this SynchronousHub hub, EndpointRegistration registration)
    {
        return registration.IsHandler
            ? hub.AddHandler(registration.Config, registration.Handler!)
            : hub.AddEndpoint(registration.Config, registration.EndpointType!);
    }
}
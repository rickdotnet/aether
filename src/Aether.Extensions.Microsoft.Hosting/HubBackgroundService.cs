using Aether.Abstractions.Messaging;
using Aether.Abstractions.Providers;
using Aether.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RickDotNet.Extensions.Base;

namespace Aether.Extensions.Microsoft.Hosting;

internal sealed class HubBackgroundService : BackgroundService, IAsyncDisposable
{
    private readonly IEnumerable<HubRegistration> hubRegistrations;
    private readonly AetherClient aether;
    private readonly ILogger<HubBackgroundService> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly List<IAetherEndpoint> endpoints = [];

    public HubBackgroundService(
        IServiceProvider serviceProvider,
        AetherClient aether,
        ILogger<HubBackgroundService> logger
    )
    {
        this.serviceProvider = serviceProvider;
        this.aether = aether;
        this.logger = logger;

        hubRegistrations = serviceProvider.GetRequiredService<IEnumerable<HubRegistration>>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("HubBackgroundService is starting.");

        try
        {
            // endpoints are provided by the application service provider
            var endpointProvider = new HostedEndpointProvider(serviceProvider);

            // during startup, we will register all hubs and endpoints
            foreach (var hubRegistration in hubRegistrations)
            {
                // hub registrations track their own provider types
                // we'll likely open it up to provider instances in the future
                var subProvider = (ISubscriptionProvider)serviceProvider.GetRequiredService(hubRegistration.SubscriptionProviderType!);
                var pubProvider = (IPublisherProvider)serviceProvider.GetRequiredService(hubRegistration.PublisherProviderType!);

                // this is the current bottleneck of the system
                // this SynchronousHub is from a previous iteration of the library
                // and will be replaced with a more flexible implementation
                var hub = new SynchronousHub(subProvider, pubProvider, endpointProvider);

                // assign the hub to the aether client
                aether.Messaging.SetHub(hubRegistration.HubName, hub);

                // register all endpoints for the hub
                foreach (var endpointRegistration in hubRegistration.EndpointRegistrations)
                {
                    var endpointName = endpointRegistration.Config.EndpointName;
                    
                    // an endpoint must have a type or a handler
                    var validRegistration = endpointRegistration.Validate();
                    await validRegistration.ResolveAsync(
                        onError: error =>
                        {
                            logger.LogWarning("{EndpointName} - Skipping registration.", endpointName);
                            logger.LogDebug("{EndpointName} - {Error}", endpointName, error);
                            
                            return Task.CompletedTask;
                        },
                        onSuccess: async _ =>
                        {
                            logger.LogInformation("Creating Endpoint - {EndpointName}.", endpointName);
                            var endpoint = hub.CreateEndpoint(endpointRegistration);
                            endpoints.Add(endpoint);

                            logger.LogInformation("Starting Endpoint - {EndpointName}.", endpointName);
                            await endpoint.StartEndpoint(stoppingToken);
                            logger.LogDebug("Endpoint ({EndpointName}) started successfully.", endpointName);
                        });
                } // foreach endpointRegistration
            } // foreach hubRegistration

            logger.LogInformation("HubBackgroundService has started.");
            await Task.Delay(-1, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("HubBackgroundService was canceled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during ExecuteAsync.");
        }

        logger.LogWarning("HubBackgroundService has finished execution.");
    }

    public async ValueTask DisposeAsync()
    {
        logger.LogInformation("HubBackgroundService is disposing.");

        foreach (var endpoint in endpoints)
        {
            try
            {
                await endpoint.DisposeAsync();
                logger.LogDebug("Endpoint disposed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while disposing an endpoint.");
            }
        }

        logger.LogInformation("ApolloBackgroundService has been disposed.");
    }
}

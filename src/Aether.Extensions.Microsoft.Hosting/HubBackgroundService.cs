using Aether.Abstractions.Messaging;
using Aether.Abstractions.Providers;
using Aether.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            foreach (var hubRegistration in hubRegistrations)
            {
                var subProvider = (ISubscriptionProvider)serviceProvider.GetRequiredService(hubRegistration.SubscriptionProviderType!);
                var pubProvider = (IPublisherProvider)serviceProvider.GetRequiredService(hubRegistration.PublisherProviderType!);

                var hub = new SynchronousHub(subProvider, pubProvider);
                aether.Messaging.SetHub(hubRegistration.HubName, hub);

                foreach (var registration in hubRegistration.Registrations)
                {
                    if (registration.EndpointType is null && registration.Handler is null)
                    {
                        logger.LogWarning("{EndpointName} - EndpointType and Handler are both null. Skipping registration.", registration.Config.EndpointName);
                        continue;
                    }

                    logger.LogInformation("Starting Endpoint - {EndpointName}.", registration.Config.EndpointName);

                    var endpoint = registration.IsHandler
                        ? hub.AddHandler(registration.Config, registration.Handler!)
                        : hub.AddEndpoint(registration.EndpointType!, registration.Config);

                    endpoints.Add(endpoint);

                    await endpoint.StartEndpoint(stoppingToken);
                    logger.LogDebug("Endpoint ({EndpointName}) started successfully.", registration.Config.EndpointName);
                }

                await Task.Delay(-1, stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("HubBackgroundService was canceled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during ExecuteAsync.");
        }

        logger.LogInformation("HubBackgroundService has finished execution.");
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

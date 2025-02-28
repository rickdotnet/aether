using Aether.Abstractions.Hosting;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Storage;
using Aether.Extensions.Microsoft.Hosting.Builders;
using Aether.Extensions.Microsoft.Hosting.Messaging;
using Aether.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RickDotNet.Extensions.Base;

namespace Aether.Extensions.Microsoft.Hosting;

internal sealed class AetherBackgroundService : BackgroundService, IAsyncDisposable
{
    private readonly IEnumerable<HubRegistration> hubRegistrations;
    private readonly IEnumerable<StorageRegistration> storageRegistrations;
    private readonly AetherClient aether;
    private readonly ILogger<AetherBackgroundService> logger;
    private readonly IServiceProvider serviceProvider;

    public AetherBackgroundService(
        IServiceProvider serviceProvider,
        AetherClient aether,
        ILogger<AetherBackgroundService> logger
    )
    {
        this.serviceProvider = serviceProvider;
        this.aether = aether;
        this.logger = logger;

        hubRegistrations = serviceProvider.GetRequiredService<IEnumerable<HubRegistration>>();
        storageRegistrations = serviceProvider.GetRequiredService<IEnumerable<StorageRegistration>>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("HubBackgroundService is starting.");

        try
        {
            logger.LogInformation("Bootstrapping messaging.");
            await BootstrapMessaging(stoppingToken);

            logger.LogInformation("Bootstrapping storage.");
            await BootstrapStorage();

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

    private async Task BootstrapStorage()
    {
        foreach (var storageRegistration in storageRegistrations)
        {
            var storageProviderFactory =
                (IStoreProvider)serviceProvider.GetRequiredService(storageRegistration.ProviderFactoryType!);
            aether.Storage.SetStore(
                storageRegistration.StoreName,
                await storageProviderFactory.CreateStore(storageRegistration)
            );
        }
    }

    private async Task BootstrapMessaging(CancellationToken stoppingToken)
    {
        // endpoints are provided by the application service provider
        var endpointProvider = new HostedEndpointProvider(serviceProvider);

        // during startup, we will register all hubs and endpoints
        foreach (var hubRegistration in hubRegistrations)
        {
            // hub registrations track their own provider types
            // we'll likely open it up to provider instances in the future
            var subProvider =
                (ISubscriptionProvider)serviceProvider.GetRequiredService(hubRegistration.SubscriptionProviderType!);
            var pubProvider =
                (IPublisherProvider)serviceProvider.GetRequiredService(hubRegistration.PublisherProviderType!);

            var hub = new ChannelBackedHub(subProvider, pubProvider, endpointProvider);

            // assign the hub to the aether client
            aether.Messaging.SetHub(hubRegistration.HubName, hub);

            // register all endpoints for the hub
            foreach (var endpointRegistration in hubRegistration.EndpointRegistrations)
            {
                var endpointName = endpointRegistration.Config.EndpointName ?? endpointRegistration.Config.Subject;

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
                        await hub.CreateEndpoint(endpointRegistration);
                    });
            } // foreach endpointRegistration

            await hub.Start(stoppingToken);
        } // foreach hubRegistration
    }

    public ValueTask DisposeAsync()
    {
        logger.LogInformation("HubBackgroundService is disposing.");


        return ValueTask.CompletedTask;
    }
}
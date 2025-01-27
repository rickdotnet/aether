using Aether;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Extensions.Microsoft.Hosting;
using Aether.Providers.NATS;
using Aether.Providers.NATS.Messaging;
using ConsoleDemo.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.JetStream.Models;

namespace ConsoleDemo.Demos;

public class HostDemo
{
    public static async Task Run()
    {
        #region docs-snippet-host

        var consumer = new ConsumerConfig("consumer-name");
        var durableConfig =
            InstanceEndpoint.EndpointConfig
                .WithConsumer(consumer);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddAether(
            ab => ab.Messaging
                .AddHub(hub => hub
                    .UseMemory()
                    .AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle)) // replace default hub
                // .AddHub("nats", // named hub
                //     hub => hub
                //         .UseNats()
                //         .AddEndpoint<InstanceEndpoint>(durableConfig)
                //         .AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle)
                // )
        );

        #endregion

        var host = builder.Build();
        var hostTask = host.RunAsync();

        // give everything time to start up
        await Task.Delay(3000);
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        #region docs-snippet-publish

        var aether = serviceProvider.GetRequiredService<AetherClient>();
        var publisher = aether.Messaging.CreatePublisher(StaticEndpoint.EndpointConfig);

        await Task.WhenAll(
            publisher.Send(new SomethingHappenedCommand("test 1"), CancellationToken.None),
            publisher.Send(new SomethingHappenedCommand("test 2"), CancellationToken.None),
            publisher.Send(new SomethingHappenedCommand("test 3"), CancellationToken.None),
            publisher.Send(new SomethingHappenedCommand("test 4"), CancellationToken.None),
            publisher.Send(new SomethingHappenedCommand("test 5"), CancellationToken.None)
        );

        #endregion

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }
}

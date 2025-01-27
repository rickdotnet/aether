using Aether;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Extensions.Microsoft.Hosting;
using Aether.Providers.NATS;
using Aether.Providers.NATS.Messaging;
using ConsoleDemo.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.JetStream.Models;
using NATS.Extensions.Microsoft.DependencyInjection;
using RickDotNet.Extensions.Base;

namespace ConsoleDemo.Demos;

public class HostDemo
{
    public enum ExampleType
    {
        ExampleOne,
        ExampleTwo,
        ExampleThree
    }

    public static async Task Run(ExampleType exampleType = ExampleType.ExampleOne)
    {
        #region docs-snippet-host

        var consumer = new ConsumerConfig("consumer-name");
        var durableConfig =
            InstanceEndpoint.EndpointConfig
                .WithConsumer(consumer);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddNatsClient(nats => nats.ConfigureOptions(opts => opts with { Url = "nats://localhost:4222" }));
        builder.Services.AddSingleton<InstanceEndpoint>();


        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (exampleType)
        {
            // static - in memory default
            case ExampleType.ExampleOne:
                ExampleOne(builder);
                break;

            // static - nats default
            case ExampleType.ExampleTwo:
                ExampleTwo(builder);
                break;

            // static - in memory default, instance - nats
            case ExampleType.ExampleThree:
                Example3(builder, durableConfig);
                break;
        }

        #endregion

        var host = builder.Build();
        var hostTask = host.RunAsync();

        // give everything time to start up
        await Task.Delay(3000);
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        #region docs-snippet-publish

        var aether = serviceProvider.GetRequiredService<AetherClient>();

        // static publisher is created from default hub
        var staticPublisher = aether.Messaging.CreatePublisher(StaticEndpoint.EndpointConfig);
        var natsPublisher = aether.Messaging.GetHub("nats").Select(hub => hub.CreatePublisher(durableConfig)).ValueOrDefault();


        await Task.WhenAll(
            staticPublisher.Send(new SomethingHappenedCommand("test 1"), CancellationToken.None),
            staticPublisher.Send(new SomethingHappenedCommand("test 2"), CancellationToken.None),
            staticPublisher.Send(new SomethingHappenedCommand("test 3"), CancellationToken.None),
            staticPublisher.Send(new SomethingHappenedCommand("test 4"), CancellationToken.None),
            staticPublisher.Send(new SomethingHappenedCommand("test 5"), CancellationToken.None),
            (natsPublisher?.Send(new SomethingHappenedCommand("instance test"), CancellationToken.None) ?? Task.CompletedTask)
        );

        #endregion

        await Task.Delay(1000);
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }

    private static void ExampleOne(HostApplicationBuilder builder)
    {
        builder.Services.AddAether(
            ab => ab.Messaging
                .AddHub(hub => hub // no name replaces default hub
                    .UseMemory() // optional, default is memory
                    .AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle))
        );
    }

    private static void ExampleTwo(HostApplicationBuilder builder)
    {
        builder.Services.AddAether(
            ab => ab.Messaging
                .AddHub(
                    hub => hub    // no name replaces default hub
                        .UseNats() // default is now nats, instead of memory
                        .AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle)
                ));
    }

    private static void Example3(HostApplicationBuilder builder, EndpointConfig durableConfig)
    {
        builder.Services.AddAether(
            ab => ab.Messaging
                .AddHub(hub => hub // default is memory
                    .AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle))
                .AddHub("nats", // named hub for nats
                    hub => hub
                        .UseNats()
                        .AddEndpoint<InstanceEndpoint>(durableConfig)
                )
        );
    }
}

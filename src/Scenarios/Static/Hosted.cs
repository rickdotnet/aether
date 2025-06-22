using Aether;
using Aether.Abstractions.Messaging;
using Aether.Extensions.Microsoft.Hosting;
using Aether.Providers.NATS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Extensions.Microsoft.DependencyInjection;
using RickDotNet.Extensions.Base;
using Scenarios.Endpoints;

namespace Scenarios;

public class Hosted
{
    public static async Task Run()
    {
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        services.AddNatsClient(nats =>
        {
            nats.ConfigureOptions(opts => opts with
            {
                Url = "nats://localhost:4222",
            });
        });
        services.AddAether(aether =>
        {
            aether.Messaging
                .AddNatsHub(hub => hub
                    .AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle)
                    .AddHandler(new EndpointConfig("static.endpoint2"), StaticEndpoint.Handle)
                    .AddHandler(new EndpointConfig("static.endpoint2"), StaticEndpoint.Handle, HandlerConfig.Concurrent(2))
                );
            
            // creates a named store and keeps the default store as memory
            aether.Storage.AddNatsStore("nats"); 
        });

        var host = builder.Build();
        var hostTask = host.StartAsync();

        var aether = host.Services.GetRequiredService<AetherClient>();
        await aether.Storage.Upsert("test", "storage test");
        var natsStore = aether.Storage.GetStore("nats").ValueOrDefault() ?? throw new Exception("NATS store not found");
        await natsStore.Upsert("test", "nats test");

        var messaging = aether.Messaging;
        await messaging.Send(
            AetherMessage.For(
                "static.endpoint",
                new SomethingHappenedCommand("Oh you didn't KNOW???")
            )
        );

        var response = await messaging.Request(
            AetherMessage.For(
                "static.endpoint",
                new TestRequest("test")),
            CancellationToken.None
        );

        response.Resolve(
            onSuccess: data => Console.WriteLine($"Response: {data.As<string>()}"),
            onError: error => Console.WriteLine($"Error: {error}")
        );

        var storageResult = await aether.Storage.Get<string>("test");
        storageResult.Resolve(
            onSuccess: data => Console.WriteLine($"Storage test: {data}"),
            onError: error => Console.WriteLine($"Error: {error}")
        );

        var natsStoreResult = await natsStore.Get<string>("test");
        natsStoreResult.Resolve(
            onSuccess: data => Console.WriteLine($"NATS Storage test: {data}"),
            onError: error => Console.WriteLine($"Error: {error}")
        );

        for (var i = 0; i < 20; i++)
        {
            await messaging.Send(
                AetherMessage.For(
                    "static.endpoint",
                    new SomethingHappenedCommand($"Message {i}")
                )
            );

            await messaging.Send(
                AetherMessage.For(
                    "static.endpoint2",
                    new SomethingHappenedCommand($"Message {i}")
                )
            );
        }

        await Task.Delay(25000);
        await hostTask;
    }
}

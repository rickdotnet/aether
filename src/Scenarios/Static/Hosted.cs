using Aether;
using Aether.Abstractions.Messaging;
using Aether.Extensions.Microsoft.Hosting;
using Aether.Providers.NATS;
using Aether.Providers.NATS.Messaging;
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
            aether.Messaging
                .AddNatsHub(hub => hub
                    .AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle)
                    .AddHandler(new EndpointConfig("static.endpoint2"), StaticEndpoint.Handle)
                )
        );

        var host = builder.Build();
        var hostTask = host.StartAsync();

        var aether = host.Services.GetRequiredService<AetherClient>();
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

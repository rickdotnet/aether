using Aether;
using Aether.Abstractions.Messaging;
using Aether.Messaging;
using Aether.Providers.Memory;
using Aether.Providers.NATS.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
using RickDotNet.Extensions.Base;
using Scenarios.Endpoints;

namespace Scenarios;

public class Nats
{
    public static async Task Run()
    {
        var connection = new NatsConnection(NatsOpts.Default with
        {
            Url = "nats://localhost:4222",
        });
        var natsHub = new NatsHub(connection, NullLogger<NatsHub>.Instance);
        var natsStore = new MemoryStore();

        var aetherHub = new AetherHub(natsHub);

        var client = new AetherClient(
            aetherHub,
            //natsHub,
            natsStore
        );

        client.Messaging.AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle);
        client.Messaging.AddHandler(new EndpointConfig("static.endpoint2"), StaticEndpoint.Handle);

        var messaging = client.Messaging;
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

        Console.WriteLine();
        await Task.Delay(5000);
        
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
    }
}

using Aether;
using Aether.Abstractions.Messaging;
using Aether.Messaging;
using Aether.Providers.Memory;
using RickDotNet.Extensions.Base;
using Scenarios.Endpoints;

namespace Scenarios;

public class Memory
{
    public static async Task Run()
    {
        var memoryHub = new MemoryHub();
        var memoryStore = new MemoryStore();

        var aetherHub = new AetherHub(memoryHub);
        
        var client = new AetherClient(
            aetherHub,
            memoryStore
        );
        
        await client.Storage.Upsert("test", "storage test");
        
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

        var storageResult = await client.Storage.Get<string>("test");
        storageResult.Resolve(
            onSuccess: data => Console.WriteLine($"Storage test: {data}"),
            onError: error => Console.WriteLine($"Error: {error}")
        );
        
        for (var i = 0; i < 10; i++)
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
        
        await Task.Delay(20000);
    }
}

public record TestRequest(string Message) : IRequest<string>;

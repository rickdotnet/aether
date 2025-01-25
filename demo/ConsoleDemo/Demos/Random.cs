using Aether;
using Aether.Abstractions.Messaging.Configuration;
using ConsoleDemo.Endpoints;
using RickDotNet.Extensions.Base;

namespace ConsoleDemo.Demos;

public class Random
{
    public static async Task Run()
    {
        var aether = AetherClient.MemoryClient;
        var storage = aether.Storage;
        var messaging = aether.Messaging;

        // storage can be KV, SQL, or any other storage provider
        var somethingHappened = new SomethingHappenedCommand("If it happened, it wasn't in KV!");
        var somethingFromKv = await storage.Get("someKey");
        somethingFromKv
            .Select(data => data.As<SomethingHappenedCommand>())
            .OnSuccess(something => somethingHappened = something);

        // grab dynamic endpoint config from KV
        var instanceConfig = InstanceEndpoint.EndpointConfig;
        var configFromKv = await storage.Get("instance-config");
        configFromKv
            .Select(data => data.As<EndpointConfig>())
            .OnSuccess(endpointConfig => instanceConfig = endpointConfig);

        // endpoint option 1 - from service provider
        await using var endpointProvider = messaging.AddEndpoint<InstanceEndpoint>(instanceConfig);

        // endpoint option 2 - from instance
        var endpoint = new InstanceEndpoint();
        await using var endpointFromInstance = messaging.AddEndpoint(instanceConfig, endpoint);

        // static option 1
        await using var handlerMethod = messaging.AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle);

        // static option 2
        await using var anonHandler = messaging.AddHandler(StaticEndpoint.EndpointConfig, (context, _) =>
        {
            var theThing = context.Data.As<SomethingHappenedCommand>()!;
            Console.WriteLine($"Received message: {theThing.Message}");

            var equal = theThing == somethingHappened;
            Console.WriteLine($"Equal: {equal}");

            return Task.CompletedTask;
        });

        // send a message - in process or out of process
        var publisher = messaging.CreatePublisher(StaticEndpoint.EndpointConfig);
        await publisher.Send(somethingHappened);
    }
}
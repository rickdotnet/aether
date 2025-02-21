using Aether;
using Aether.Abstractions.Messaging;
using RickDotNet.Extensions.Base;
using Scenarios.Endpoints;

namespace Scenarios;

public class Memory
{
    public static async Task Run()
    {
        var somethingHappened = new SomethingHappenedCommand("Oh you didn't KNOW??? Your ASS better call somebody!");

        var endpointProvider = new GenericEndpointProvider();
        var client = AetherClient.CreateMemoryClient(endpointProvider);

        var storage = client.Storage;
        var item = await storage.Insert("test", "my-string");
        var item2 = await storage.Insert("test-2", somethingHappened);

        var outItem = await storage.Get<string>("test", CancellationToken.None);
        var outItem2 = await storage.Get<SomethingHappenedCommand>("test-2", CancellationToken.None);

        Console.WriteLine(outItem);
        Console.WriteLine(outItem2);
        
        await client.Messaging.AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle);
        await client.Messaging.AddEndpoint<InstanceEndpoint>(InstanceEndpoint.EndpointConfig);
        await client.Messaging.AddEndpoint<SecondEndpoint>(SecondEndpoint.EndpointConfig);

        // new
        await client.Messaging.Start(CancellationToken.None);

        // send a message - in process or out of process
        var staticPublisher = client.Messaging.CreatePublisher(StaticEndpoint.EndpointConfig);
        var instancePublisher = client.Messaging.CreatePublisher(InstanceEndpoint.EndpointConfig);
        var secondPublisher = client.Messaging.CreatePublisher(SecondEndpoint.EndpointConfig);

        await staticPublisher.Send(somethingHappened);
        await instancePublisher.Send(somethingHappened);


        await Task.Delay(1000);

        // await staticPublisher.Send(somethingHappened);
        // await instancePublisher.Send(somethingHappened);

        var data = AetherData.Serialize(somethingHappened);
        await staticPublisher.Send<SomethingHappenedCommand>(data);
        await instancePublisher.Send<SomethingHappenedCommand>(data);
        await secondPublisher.Send<SomethingHappenedCommand>(data);

        var result1 = await instancePublisher.Request<string>(AetherData.Serialize("test"), CancellationToken.None);
        var result2 = await instancePublisher.Request<TestRequest, string>(new TestRequest("test"), cancellationToken: CancellationToken.None);
        var result3 = await secondPublisher.Request<TestRequest, string>(new TestRequest("test"), cancellationToken: CancellationToken.None);
        var result4 = await secondPublisher.Request<TestRequest, string>(new TestRequest("test"), cancellationToken: CancellationToken.None);

        Console.WriteLine(result1.ValueOrDefault()?.As<string>() ?? "null");
        Console.WriteLine(result2 ?? "null");
        
        Console.WriteLine(result3);
        Console.WriteLine(result4);
        
        await Task.Delay(1000);
    }

    public record TestRequest(string Message) : IRequest<string>;

    class GenericEndpointProvider : IEndpointProvider
    {
        private static InstanceEndpoint instanceEndpoint = new();
        private static SecondEndpoint secondEndpoint = new();

        public object? GetService(Type endpointType) => endpointType switch
        {
            _ when endpointType == typeof(InstanceEndpoint) => instanceEndpoint,
            _ when endpointType == typeof(SecondEndpoint) => secondEndpoint,
            _ => null
        };

        public T? GetService<T>() => (T?)GetService(typeof(T));
    }
}

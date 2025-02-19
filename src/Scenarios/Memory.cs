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

        await client.Messaging.AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle);
        await client.Messaging.AddEndpoint<InstanceEndpoint>(InstanceEndpoint.EndpointConfig);

        // new
        await client.Messaging.Start(CancellationToken.None);

        // send a message - in process or out of process
        var staticPublisher = client.Messaging.CreatePublisher(StaticEndpoint.EndpointConfig);
        var instancePublisher = client.Messaging.CreatePublisher(InstanceEndpoint.EndpointConfig);

        await staticPublisher.Send(somethingHappened);
        await instancePublisher.Send(somethingHappened);


        await Task.Delay(1000);

        // await staticPublisher.Send(somethingHappened);
        // await instancePublisher.Send(somethingHappened);

        var data = AetherData.Serialize(somethingHappened);
        await staticPublisher.Send<SomethingHappenedCommand>(data);
        await instancePublisher.Send<SomethingHappenedCommand>(data);

        var result1 = await instancePublisher.Request<string>(AetherData.Serialize("test"), CancellationToken.None);
         if (result1)
             Console.WriteLine("Success");

        var result2 = await instancePublisher.Request<TestRequest,string>(
            new TestRequest("test"),
            cancellationToken: CancellationToken.None);

        Console.WriteLine(result1.ValueOrDefault()?.As<string>());
        Console.WriteLine(result2);
        await Task.Delay(1000);
    }

    public record TestRequest(string Message) : IRequest<string>;

    class GenericEndpointProvider : IEndpointProvider
    {
        private static InstanceEndpoint instanceEndpoint = new();

        public object? GetService(Type endpointType) => instanceEndpoint;

        public T? GetService<T>() => default;
    }
}

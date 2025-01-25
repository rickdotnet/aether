using Aether;
using ConsoleDemo.Endpoints;

namespace ConsoleDemo.Demos;

public class InMemory
{
    public static async Task Run()
    {
        var somethingHappened = new SomethingHappenedCommand("Oh you didn't KNOW??? Your ASS better call somebody!");

        var client = AetherClient.MemoryClient;

        var staticEndpoint = client.Messaging.AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle);
        await staticEndpoint.StartEndpoint(CancellationToken.None);

        var instance = new InstanceEndpoint();
        var instanceEndpoint = client.Messaging.AddEndpoint(InstanceEndpoint.EndpointConfig, instance);
        await instanceEndpoint.StartEndpoint(CancellationToken.None);

        // send a message - in process or out of process
        var staticPublisher = client.Messaging.CreatePublisher(StaticEndpoint.EndpointConfig);
        var instancePublisher = client.Messaging.CreatePublisher(InstanceEndpoint.EndpointConfig);

        await staticPublisher.Send(somethingHappened);
        await instancePublisher.Send(somethingHappened);

        await Task.Delay(1000);

        await staticPublisher.Send(somethingHappened);
        await instancePublisher.Send(somethingHappened);

        await Task.Delay(1000);
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

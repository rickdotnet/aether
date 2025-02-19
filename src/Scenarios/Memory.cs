using Aether;
using Scenarios.Endpoints;

namespace Scenarios;

public class Memory
{
    public static async Task Run()
    {
        var somethingHappened = new SomethingHappenedCommand("Oh you didn't KNOW??? Your ASS better call somebody!");

        var client = AetherClient.MemoryClient;

        await client.Messaging.AddHandler(StaticEndpoint.EndpointConfig, StaticEndpoint.Handle);

        // new
        await client.Messaging.Start(CancellationToken.None);
        
        // send a message - in process or out of process
        var staticPublisher = client.Messaging.CreatePublisher(StaticEndpoint.EndpointConfig);

        await staticPublisher.Send(somethingHappened);

        await Task.Delay(1000);

        // await staticPublisher.Send(somethingHappened);
        // await instancePublisher.Send(somethingHappened);

        var data = AetherData.Serialize(somethingHappened);
        await staticPublisher.Send<SomethingHappenedCommand>(data, "send");

        await Task.Delay(1000);
    }
}

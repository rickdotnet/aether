using Aether;
using Aether.Providers.NATS.Messaging;
using ConsoleDemo.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
using Serilog;

namespace ConsoleDemo.Demos;

public class Nats
{
    public static async Task Run()
    {
        var somethingHappened = new SomethingHappenedCommand("Oh you didn't KNOW??? Your ASS better call somebody!");
        
        
        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(logger);
        });
        
        var natsOpts = NatsOpts.Default with { Url = "nats://127.0.0.1:4222" };
        var natsConnection = new NatsConnection(natsOpts);
        var subscriptionProvider = new NatsSubscriptionProvider(natsConnection, loggerFactory);
        var publisher = new NatsPublisher(natsConnection);
        var client = AetherClient.CreateClient(subscriptionProvider, publisher);

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

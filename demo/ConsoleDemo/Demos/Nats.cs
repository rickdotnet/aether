using Aether;
using Aether.Providers.NATS.Messaging;
using ConsoleDemo.Endpoints;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using Serilog;

namespace ConsoleDemo.Demos
{
    public class Nats
    {
        public static async Task Run()
        {
            // client
            var (client, loggerFactory) = Initialize();

            // demo command - DX style
            var somethingHappened = new SomethingHappenedCommand("Oh you didn't KNOW??? Your ASS better call somebody!");

            // endpoints
            await StaticSetup(client, loggerFactory);
            await InstanceSetup(client);

            // publish demo
            await PublishMessages(client, somethingHappened);

            // stare at the audience
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static (AetherClient, ILoggerFactory) Initialize(string natsUrl = "nats://localhost:4222")
        {
            // logger
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(logger);
            });

            // nats
            var natsOpts = NatsOpts.Default with { Url = natsUrl };
            var natsConnection = new NatsConnection(natsOpts);
            var subscriptionProvider = new NatsSubscriptionProvider(natsConnection, loggerFactory);
            var publisher = new NatsPublisher(natsConnection);

            var client = AetherClient.CreateClient(subscriptionProvider, publisher);

            return (client, loggerFactory);
        }

        private static async Task StaticSetup(AetherClient client, ILoggerFactory loggerFactory)
        {
            var consumerConfig = new ConsumerConfig("static-consumer"); // Expects a stream named: static_endpoint
            var staticConfig = StaticEndpoint.EndpointConfig.WithConsumer(consumerConfig);

            var staticEndpoint = client.Messaging.AddHandler(staticConfig, StaticEndpoint.Handle);

            await staticEndpoint.StartEndpoint(CancellationToken.None);
        }

        private static async Task InstanceSetup(AetherClient client)
        {
            var instance = new InstanceEndpoint();
            var instanceEndpoint = client.Messaging.AddEndpoint(InstanceEndpoint.EndpointConfig, instance);
            await instanceEndpoint.StartEndpoint(CancellationToken.None);
        }

        private static async Task PublishMessages(AetherClient client, SomethingHappenedCommand command)
        {
            var staticPublisher = client.Messaging.CreatePublisher(StaticEndpoint.EndpointConfig);
            var instancePublisher = client.Messaging.CreatePublisher(InstanceEndpoint.EndpointConfig);

            // Fire and forget for good measure.
            await staticPublisher.Send(command);
            await instancePublisher.Send(command);
            await Task.Delay(1000);
            await staticPublisher.Send(command);
            await instancePublisher.Send(command);
        }
    }
}
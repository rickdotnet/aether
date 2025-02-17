using Aether;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Extensions.Microsoft.Hosting;
using Aether.Providers.NATS;
using ConsoleDemo.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Extensions.Microsoft.DependencyInjection;

namespace ConsoleDemo.Demos;


public static class WildCardDemo
{
    static EndpointConfig WildCardConfig = new()
    {
        EndpointName = "Wildcard Endpoint",
        Subject = "test.*.wildcard",
    };
    public static async Task Run()
    {
        var builder = Host.CreateApplicationBuilder();

        var wtf = new EndpointConfig
        {
            EndpointName = "WTF",
            Subject = "testing.the.wildcard",
        };
        builder.Services.AddNatsClient(nats => nats.ConfigureOptions(opts => opts with { Url = "nats://localhost:4222" }));
        builder.Services.AddSingleton<WildCardEndpoint>();
        builder.Services.AddAether(
            ab =>
            {
                ab.Messaging.AddHub(hub => hub
                    .UseNats()
                    .AddEndpoint<WildCardEndpoint>(
                        //WildCardConfig
                      wtf
                        // new EndpointConfig
                        // {
                        //     EndpointName = "WTF",
                        //     Subject = "testing.the.wildcard",
                        // }
                    ));
            });
        
        var host = builder.Build();
        _ = host.RunAsync(); // keep it running

        // give everything time to start up
        await Task.Delay(5000);
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        
        var aether = serviceProvider.GetRequiredService<AetherClient>();
        
        var publisher = aether.Messaging.CreatePublisher(new PublishConfig
        {
            Subject = "test.the.wildcard"
        });
        
        var alternatePublisher = aether.Messaging.CreatePublisher(new PublishConfig()
        {
            Subject = "test.another.wildcard"
        });
        
        await publisher.Send(new SomethingHappenedCommand("Message One"));
        await Task.Delay(1000);
        await alternatePublisher.Send(new SomethingHappenedCommand("Message Two"));
        await Task.Delay(10000);
    }
}

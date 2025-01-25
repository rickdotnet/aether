using Aether.Abstractions.Messaging;
using Aether.Abstractions.Providers;
using Aether.Messaging.Configuration;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;

namespace Aether.Providers.NATS.Messaging;

public class NatsPublisher : IPublisherProvider
{
    private readonly INatsConnection connection;

    public NatsPublisher(INatsConnection connection)
    {
        this.connection = connection;
    }

    public Task Publish(PublishConfig publishConfig, AetherMessage message, CancellationToken cancellationToken)
    {
        var subject = DefaultSubjectTypeMapper.From(publishConfig).Subject;

        // add exception handling
        // broken connections don't 
        // always seem to get re-established
        // need a way to re-establish the connection
        return connection.PublishAsync(
            $"{subject}",
            message.Data?.ToArray() ?? [],
            headers: new NatsHeaders((Dictionary<string, StringValues>)message.Headers),
            cancellationToken: cancellationToken).AsTask();
    }

    public async Task<byte[]> Request(
        PublishConfig publishConfig,
        AetherMessage message,
        CancellationToken cancellationToken)
    {
        var subject = DefaultSubjectTypeMapper.From(publishConfig).Subject;

        var response = await connection
            .RequestAsync<byte[], byte[]>(
                $"{subject}",
                message.Data?.ToArray() ?? [],
                headers: new NatsHeaders((Dictionary<string, StringValues>)message.Headers),
                cancellationToken: cancellationToken).AsTask();
        return response.Data!;
    }
}
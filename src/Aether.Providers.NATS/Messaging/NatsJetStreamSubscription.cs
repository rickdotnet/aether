using Aether.Abstractions.Messaging;
using Aether.Messaging;
using Aether.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using ConsumerConfig = NATS.Client.JetStream.Models.ConsumerConfig;

namespace Aether.Providers.NATS.Messaging;

internal class NatsJetStreamSubscription : ISubscription
{
    private readonly INatsConnection connection;
    private readonly ILogger<NatsJetStreamSubscription> logger;
    private readonly SubscriptionConfig config;
    private readonly Func<MessageContext, CancellationToken, Task> handler;
    private readonly string endpointSubject;
    private readonly Dictionary<string, Type> subjectTypeMapping;

    public NatsJetStreamSubscription(
        INatsConnection connection,
        ILogger<NatsJetStreamSubscription> logger,
        SubscriptionConfig config,
        Func<MessageContext, CancellationToken, Task> handler
    )
    {
        this.connection = connection;
        this.logger = logger;
        this.config = config;
        this.handler = handler;

        var subjectTypeMapper = DefaultSubjectTypeMapper.From(config);
        endpointSubject = subjectTypeMapper.Subject;
        subjectTypeMapping = subjectTypeMapper.SubjectTypeMapping;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        try
        {
            var js = new NatsJSContext((NatsConnection)connection);
        
            var streamNameClean = endpointSubject.CleanStreamName();
        
            //// Creating resources isn't supported yet
            // logger.LogWarning("Create Missing Resources? {CreateMissingResources}", config.CreateMissingResources);
            // if (config.CreateMissingResources)
            // {
            //     logger.LogTrace("Creating stream {StreamName} for {Subjects}", streamNameClean,
            //         endpointSubject);
            //     await js.CreateStreamAsync(
            //         new StreamConfig(streamNameClean, new[] { endpointSubject }),
            //         cancellationToken);
            // }
        
            logger.LogTrace("Creating consumer {ConsumerName} for stream {StreamName}", config.ConsumerConfig.Name,
                streamNameClean);
        
            var consumerConfig = new ConsumerConfig(config.ConsumerConfig.Name);
            
            // TODO: build config based on aether consumer config
            var consumer = await js.CreateOrUpdateConsumerAsync(streamNameClean, consumerConfig, cancellationToken);
        
            logger.LogInformation("Subscribing to {Subject}", endpointSubject);
            await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken))
            {
                var handlerOnly = config.EndpointType == null;
                try
                {
                    var subjectMapping = "";
                    if (msg.Headers != null && msg.Headers.TryGetValue(MessageHeader.SubjectMapping, out var aetherType))
                        subjectMapping = aetherType.First() ?? "";
        
                    if (handlerOnly || subjectTypeMapping.ContainsKey(subjectMapping))
                    {
                        if (config.ConsumerConfig.AckStrategy == AckStrategy.AutoAck)
                            await msg.AckAsync(cancellationToken: cancellationToken);
        
                        await ProcessMessage(msg);
        
                        if (config.ConsumerConfig.AckStrategy == AckStrategy.Default)
                            await msg.AckAsync(cancellationToken: cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning(
                            "No handler found for {Subject} in endpoint ({Endpoint})",
                            subjectMapping,
                            config.EndpointName);
        
                        await msg.AckTerminateAsync(cancellationToken: cancellationToken);
                    }
                }
        
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message from {Subject}", msg.Subject);
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("TaskCanceledException");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error subscribing to {EndpointSubject}", endpointSubject);
        }
        
        return;

        Task ProcessMessage(NatsJSMsg<byte[]> natsMsg)
        {
            var message = new AetherMessage
            {
                Headers = natsMsg.Headers ?? new NatsHeaders(),
                Data = natsMsg.Data ?? [],
            };

            var subjectMapping = "";
            if (natsMsg.Headers != null && natsMsg.Headers.TryGetValue(MessageHeader.SubjectMapping, out var aetherType))
                subjectMapping = aetherType.First() ?? "";

            subjectTypeMapping.TryGetValue(subjectMapping, out var messageType);
            message.MessageType = messageType ?? typeof(byte[]);

            var replyFunc = natsMsg.ReplyTo != null
                ? new Func<byte[], CancellationToken, Task>(
                    (response, innerCancel) =>
                        connection.PublishAsync(natsMsg.ReplyTo, response, cancellationToken: innerCancel).AsTask()
                )
                : null;

            return handler(new MessageContext(message, replyFunc), cancellationToken);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
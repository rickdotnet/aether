using System.Collections.Concurrent;
using System.Threading.Channels;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Abstractions.Providers;

namespace Aether.Messaging;

public class ChannelBackedHub : IMessageHub
{
    private readonly Channel<MessageContext> messageChannel;
    private readonly int maxWorkers;
    private readonly ISubscriptionProvider subProvider;
    private readonly IPublisherProvider publisherProvider;
    private readonly IEndpointProvider? endpointProvider;
    private readonly List<Task> workerTasks;
    private readonly List<SubscriptionContext> subscriptions = new();
    private readonly ConcurrentDictionary<string, EndpointInvoker> invokers = new();
    private CancellationTokenSource? cts;

    public ChannelBackedHub(
        ISubscriptionProvider subProvider,
        IPublisherProvider publisherProvider,
        IEndpointProvider? endpointProvider = null,
        int maxWorkers = 1,
        int bufferCapacity = 100
    )
    {
        // Dependency injection for flexibility
        this.subProvider = subProvider;
        this.publisherProvider = publisherProvider;
        this.endpointProvider = endpointProvider;

        // Configure the channel with some backpressure capacity
        messageChannel = Channel.CreateBounded<MessageContext>(bufferCapacity);

        this.maxWorkers = maxWorkers;
        workerTasks = new List<Task>();
    }
    
    public Task Start(CancellationToken cancellationToken)
    {
        // TODO: don't allow starting twice
        
        cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        for (var i = 0; i < maxWorkers; i++)
        {
            workerTasks.Add(Task.Run(() => ProcessMessagesAsync(cts.Token), cts.Token)); // do we want the second token?
        }

        foreach (var subscription in subscriptions)
        {
            var sub = subProvider.AddSubscription(subscription);
            _ = sub.Subscribe(cancellationToken);
        }

        return Task.CompletedTask;
    }

    private async Task? ProcessMessagesAsync(CancellationToken cancellationToken)
    {
            await foreach (var messageContext in messageChannel.Reader.ReadAllAsync(cancellationToken))
            {
                // get endpoint type from subject
                var subject = messageContext.Message.Subject;
                if (subject is null)
                {
                    // log error
                    Console.WriteLine("No subject on message");
                    continue; // we'll signal terminate from here
                }

                var invoker = invokers.GetValueOrDefault(subject);
                if (invoker is null)
                {
                    // log error
                    Console.WriteLine($"No invoker found for {subject}");
                    continue; // we'll signal terminate from here
                }
                
                var messageType = messageContext.Message.MessageType ?? typeof(MessageContext);
                var result = await invoker.Invoke(messageType, messageContext, cancellationToken);
                await messageContext.Signal(AckSignal.Ack, cancellationToken);
            }
    }

    public async Task Stop()
    {
        messageChannel.Writer.Complete(); // Signal we're done
        await Task.WhenAll(workerTasks); // Wait for all workers to finish
    }

    private Task<AckSignal> InnerHandle(MessageContext messageContext, CancellationToken cancellationToken)
    {
        messageChannel.Writer.TryWrite(messageContext);
        return Task.FromResult(AckSignal.Ack); // auto-ack for now
    }
    
    public Task AddEndpoint<T>(EndpointConfig endpointConfig)
        => AddEndpoint(endpointConfig, typeof(T));

    public Task AddEndpoint(EndpointConfig endpointConfig, Type endpointType)
    {
        if(endpointProvider is null)
            throw new InvalidOperationException("No endpoint provider configured");
        
        var subContext = SubscriptionContext.ForEndpoint(
            endpointConfig,
            InnerHandle,
            endpointType
        );

        var added = invokers.TryAdd(subContext.SubjectMapping.Subject, new EndpointInvoker(endpointType, endpointProvider));
        subscriptions.Add(subContext);
        
        return Task.CompletedTask;
    }

    public Task AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
    {
        var subContext = SubscriptionContext.ForEndpoint(
            endpointConfig,
            InnerHandle
        );

        var added = invokers.TryAdd(subContext.SubjectMapping.Subject, new EndpointInvoker(handler));
        subscriptions.Add(subContext);
        
        return Task.CompletedTask;
    }

     public IPublisher CreatePublisher(EndpointConfig endpointConfig)
         => CreatePublisher(endpointConfig.ToPublishConfig());

     public IPublisher CreatePublisher(PublishConfig publishConfig)
     {
         return new DefaultPublisher(publishConfig, publisherProvider);
     }
}

public enum AckSignal
{
    Ack,
    Retry,
    Nak,
    DeadLetter,
    ExplicitAck, // ack in the endpoint
}

using System.Collections.Concurrent;
using System.Threading.Channels;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using RickDotNet.Extensions.Base;

namespace Aether.Messaging;

public class ChannelBackedHub : IMessageHub
{
    private readonly Channel<MessageContext> messageChannel;
    private readonly ISubscriptionProvider subProvider;
    private readonly IPublisherProvider publisherProvider;
    private readonly List<Task> workerTasks = new();
    private readonly List<SubscriptionContext> subscriptions = new();
    private readonly ConcurrentDictionary<string, EndpointInvoker> invokers = new();
    private readonly int maxWorkers;
    private readonly IEndpointProvider? endpointProvider;
    private CancellationTokenSource? cts;

    public ChannelBackedHub(
        ISubscriptionProvider subProvider,
        IPublisherProvider publisherProvider,
        IEndpointProvider? endpointProvider = null,
        int maxWorkers = 4,
        int bufferCapacity = 100
    )
    {
        messageChannel = Channel.CreateBounded<MessageContext>(bufferCapacity);

        this.subProvider = subProvider;
        this.publisherProvider = publisherProvider;
        this.endpointProvider = endpointProvider;
        this.maxWorkers = maxWorkers;
    }

    public Task Start(CancellationToken cancellationToken)
    {
        // TODO: don't allow starting twice

        cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        for (var i = 0; i < maxWorkers; i++)
            workerTasks.Add(Task.Run(() => ProcessMessagesAsync(cts.Token), cts.Token)); // do we want the second token?

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
            var subject = messageContext.Message.Subject;
            if (subject is null)
            {
                Console.WriteLine("No subject on message");
                await messageContext.Signal(AckSignal.DeadLetter, cancellationToken);
                continue;
            }

            var invoker = invokers.GetValueOrDefault(subject);
            if (invoker is null)
            {
                Console.WriteLine($"No invoker found for {subject}");
                await messageContext.Signal(AckSignal.DeadLetter, cancellationToken);
                continue; 
            }

            // messageType is currently set by the sub provider
            // by looking up the type mapping in the DefaultTypeMapper
            // this was an apollo construct that could/should go away
            // we could support more robust type resolution, but tabling
            // this for now.
            // we default to MessageContext here, which will fallback
            // to Handle(MessageContext, CancellationToken)
            var messageType = messageContext.Message.MessageType ?? typeof(MessageContext);
            var result = await invoker.Invoke(messageType, messageContext, cancellationToken);
            result.OnError(Console.WriteLine);

            // no need to ack here, since we're auto-acking for now
            // if (result)
            //await messageContext.Signal(AckSignal.Ack, cancellationToken);
        }
    }

    public async Task Stop()
    {
        messageChannel.Writer.Complete();
        await Task.WhenAll(workerTasks); 
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
        if (endpointProvider is null)
            throw new InvalidOperationException("No endpoint provider configured");

        var subContext = SubscriptionContext.ForEndpoint(
            endpointConfig,
            InnerHandle,
            endpointType
        );

        var added = invokers.TryAdd(
            subContext.SubjectMapping.Subject,
            new EndpointInvoker(endpointType, endpointProvider)
        );

        if (!added)
            Console.WriteLine("Pa Pa! The invoker wasn't added!");
        
        subscriptions.Add(subContext);

        return Task.CompletedTask;
    }

    public Task AddHandler(EndpointConfig endpointConfig, Func<MessageContext, CancellationToken, Task> handler)
    {
        var subContext = SubscriptionContext.ForEndpoint(
            endpointConfig,
            InnerHandle
        );

        var added = invokers.TryAdd(
            subContext.SubjectMapping.Subject,
            new EndpointInvoker(handler)
        );
        
        if (!added)
            Console.WriteLine("Pa Pa! The invoker wasn't added!");
        
        subscriptions.Add(subContext);

        return Task.CompletedTask;
    }

    public IPublisher CreatePublisher(EndpointConfig endpointConfig)
        => CreatePublisher(endpointConfig.ToPublishConfig());

    public IPublisher CreatePublisher(PublishConfig publishConfig)
        => new DefaultPublisher(publishConfig, publisherProvider);
}

public enum AckSignal
{
    Ack,
    Retry,
    Nak,
    DeadLetter,
    ExplicitAck, // ack in the endpoint
}

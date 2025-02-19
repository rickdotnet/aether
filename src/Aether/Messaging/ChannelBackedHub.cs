using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using RickDotNet.Base;
using RickDotNet.Extensions.Base;

namespace Aether.Messaging;

public class ChannelBackedHub : IMessageHub, IAsyncDisposable
{
    private readonly Channel<MessageContext> messageChannel;
    private readonly ISubscriptionProvider subProvider;
    private readonly IPublisherProvider publisherProvider;
    private readonly List<Task> workerTasks = new();
    private readonly List<SubscriptionContext> subscriptionContexts = new();
    private readonly ConcurrentDictionary<string, EndpointInvoker> invokers = new();
    private readonly int maxWorkers;
    private readonly IEndpointProvider? endpointProvider;
    private CancellationTokenSource? cts;
    private readonly List<ISubscription> subscriptions = new();

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
            workerTasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ProcessMessagesAsync(cts.Token)!;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing messages: {ex.Message}");
                }
                
            }, cts.Token)); // do we want the second token?

        foreach (var subscription in subscriptionContexts)
        {
            var sub = subProvider.AddSubscription(subscription);
            subscriptions.Add(sub);
            sub.Subscribe(cancellationToken);
        }

        return Task.CompletedTask;
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        await foreach (var messageContext in messageChannel.Reader.ReadAllAsync(cancellationToken))
        {
            var processResult = await Result.TryAsync(async () =>
            {
                var subject = messageContext.Message.Subject;
                if (subject is null)
                {
                    await messageContext.Signal(AckSignal.DeadLetter, cancellationToken);
                    return Result.Failure("No subject on message");
                }

                var invoker = invokers.GetValueOrDefault(subject);
                if (invoker is null)
                {
                    // POOR MANS WILDCARD MATCHING
                    // will need to rework this soon
                    //
                    // wildcard subjects contain a '*' or a '>' character
                    // if we registered a wildcard subject it won't match 1-1
                    // we need a poor-man's wildcard matching to NATS
                    // * could act like (.*) in regex, and > could act like "starts with"

                    var wildcards = invokers.Keys.Where(key => key.Contains('*')).ToList();
                    if (wildcards.Any())
                    {
                        foreach (var wildcard in wildcards)
                        {
                            var pattern = $"^{wildcard.Replace(".", "\\.").Replace("*", "(.*?)")}$";
                            invoker = invokers.FirstOrDefault(i => Regex.IsMatch(i.Key, pattern)).Value;

                            if (invoker is not null)
                                break;
                        }
                    }
                    else
                    {
                        wildcards = invokers.Keys.Where(key => key.EndsWith('>')).ToList();
                        foreach (var wildcard in wildcards)
                        {
                            invoker = invokers.FirstOrDefault(i => i.Key.StartsWith(wildcard)).Value;
                            if (invoker is not null)
                                break;
                        }
                    }
                }


                if (invoker is null)
                {
                    await messageContext.Signal(AckSignal.DeadLetter, cancellationToken);
                    return Result.Failure($"No invoker found for {subject}");
                }

                // messageType is currently set by the sub provider
                // by looking up the type mapping in the DefaultTypeMapper
                // this was an apollo construct that could/should go away
                // we could support more robust type resolution, but tabling
                // this for now.
                // we default to MessageContext here, which will fallback
                // to Handle(MessageContext, CancellationToken)
                var messageType = messageContext.Message.MessageType ?? typeof(MessageContext);
                var invokeResult = await invoker.Invoke(messageType, messageContext, cancellationToken);

                // no need to ack here, since we're auto-acking for now
                // if (invokeResult)
                //await messageContext.Signal(AckSignal.Ack, cancellationToken);
                return invokeResult;
            });

            processResult.OnError(Console.WriteLine);
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

        subscriptionContexts.Add(subContext);

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

        subscriptionContexts.Add(subContext);

        return Task.CompletedTask;
    }

    public IPublisher CreatePublisher(EndpointConfig endpointConfig)
        => CreatePublisher(endpointConfig.ToPublishConfig());

    public IPublisher CreatePublisher(PublishConfig publishConfig)
        => new DefaultPublisher(publishConfig, publisherProvider);

    public IPublisher CreatePublisher(string subject) => CreatePublisher(new PublishConfig { Subject = subject });

    public async ValueTask DisposeAsync()
    {
        if (cts is IAsyncDisposable ctsAsyncDisposable)
            await ctsAsyncDisposable.DisposeAsync();
        else if (cts != null)
            cts.Dispose();

        var disposeTasks = subscriptions.Select(sub => sub.DisposeAsync().AsTask()).ToList();
        await Task.WhenAll(disposeTasks);
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

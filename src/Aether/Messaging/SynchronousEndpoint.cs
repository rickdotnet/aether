using System.Reflection;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Abstractions.Providers;
using RickDotNet.Base;

namespace Aether.Messaging;

// TLDR: need two config values to determine
// 1. sync/async mode for message concurrency
// 2. singleton vs transient endpoint instance for each message

// the timing and message pipeline of the endpoint is TBD
// one thought is to send sync/async mode as a config option
// and use a channel to control the flow of messages
// NATS should make that easy since it already has a message queue
// and our handlers will be on the same subscription
// ASB, might be a different story

// another consideration is around the scope of each message
// in sync mode the endpoint might want to track state and thus
// would be processed one message at a time
// in async mode the caller might want to process messages in parallel
// and thus the endpoint instance be new for each message 

internal class SynchronousEndpoint : IAetherEndpoint
{
    private readonly EndpointConfig endpointConfig;
    private readonly ISubscriptionProvider subscriptionProvider;
    private readonly Type? endpointType;
    private readonly object? endpointInstance;
    private readonly Func<MessageContext, CancellationToken, Task>? handler;
    private readonly bool handlerOnly = true;
    private Task? endpointTask;
    private readonly Dictionary<Type, MethodInfo> handlers = new();

    public SynchronousEndpoint(EndpointContext endpointContext)
    {
        subscriptionProvider =
            endpointContext
                .SubscriptionProvider; // endpointConfig.SubscriptionProvider ?? throw new ArgumentException("Subscription provider is required");
        var endpointProvider = endpointContext.EndpointProvider;

        endpointConfig = endpointContext.EndpointConfig;
        handler = endpointContext.Handler;
        endpointType = endpointContext.EndpointType;

        if (endpointType is not null)
        {
            if (handler is not null)
                throw new ArgumentException("Cannot have both an endpoint type and a handler"); // or can we?

            if (endpointProvider is null)
                throw new ArgumentException("And endpoint provider is required when creating non-handler Endpoints");

            handlerOnly = false;

            // grab the instance from the DI container 
            endpointInstance = endpointProvider.GetService(endpointType);

            if (endpointInstance is null)
                throw new InvalidOperationException("Endpoint instance not found");
        }
        else if (handler is null)
        {
            throw new ArgumentException("Must have either an endpoint type or a handler");
        }
    }

    // start the endpoint
    public Task StartEndpoint(CancellationToken cancellationToken)
    {
        var subscriptionConfig = SubscriptionConfig.ForEndpoint(endpointConfig, endpointType!);

        // create the subscription
        var sub = subscriptionProvider.AddSubscription(subscriptionConfig, InternalHandle);

        // track the task for use in the future
        // prob want to track the sub and control
        // it via the subscription interface
        endpointTask = sub.Subscribe(cancellationToken);

        // let the caller go do other things
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        // sub.StopAsync()
        return ValueTask.CompletedTask;
    }

    private Task<Result<VoidResult>> InternalHandle(MessageContext context, CancellationToken cancellationToken)
    {
        if (handlerOnly)
        {
            return Result.TryAsync(() => handler!(context, cancellationToken));
        }

        var messageType = context.Message.MessageType ?? typeof(MessageContext);
        var fallbackToMessageContext = messageType == typeof(MessageContext);
        // get or set cache
        var handleMethod = handlers.GetValueOrDefault(context.Message.MessageType!);
        if (handleMethod is null)
        {
            handleMethod = fallbackToMessageContext
                ? endpointType!.GetMethod("Handle", [typeof(MessageContext), typeof(CancellationToken)])
                : endpointType!.GetMethod("Handle",
                    [context.Message.MessageType!, typeof(MessageContext), typeof(CancellationToken)]);

            if (handleMethod is null)
                return Task.FromResult(
                    Result.Failure<VoidResult>("No suitable handler found for message type")
                );

            handlers.TryAdd(messageType, handleMethod);
        }

        if (fallbackToMessageContext)
            return Result.TryAsync(() => (Task)handleMethod.Invoke(endpointInstance, [context, cancellationToken])!);

        return Result.TryAsync(async () =>
        {
            var messageObject = context.Data.As(messageType);
            // we're ok with null here, for now. need to send some tests through

            var isRequest = messageType.IsRequest();

            if (isRequest)
            {
                if (!context.ReplyAvailable)
                    return Result.Failure<VoidResult>("No reply function available");

                var response =
                    await (dynamic)handleMethod.Invoke(endpointInstance, [messageObject, context, cancellationToken])!;

                var data = AetherData.From(response);
                await context.Reply(data, cancellationToken);
            }
            else
            {
                var result = (Task)handleMethod.Invoke(endpointInstance, [messageObject, context, cancellationToken])!;
                await result;
            }

            return VoidResult.Default;
        });
    }
}
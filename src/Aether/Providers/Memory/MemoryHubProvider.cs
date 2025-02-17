using System.Collections.Concurrent;
using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;

namespace Aether.Providers.Memory;

public class MemoryHubProvider : ISubscriptionProvider, IPublisherProvider
{
    private readonly SubscriptionOptions? defaultSubscriptionOptions;
    private readonly ConcurrentDictionary<string, List<MemorySubscription>> subscriptions = new();
    private readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(30);
    
    public MemoryHubProvider()
    {
    }
    internal MemoryHubProvider(SubscriptionOptions? defaultSubscriptionOptions = null)
    {
        this.defaultSubscriptionOptions = defaultSubscriptionOptions;
    }
    
    public ISubscription AddSubscription(SubscriptionContext context)
    {
        var sub = new MemorySubscription(context.Handler, defaultSubscriptionOptions);
        var subjectMapping = DefaultSubjectTypeMapper.From(context.EndpointConfig);

        var subjectKey = subjectMapping.Subject;
        if (!subscriptions.ContainsKey(subjectKey))
            subscriptions[subjectKey] = [];

        subscriptions[subjectKey].Add(sub);
        sub.OnDispose(() => subscriptions[subjectKey].Remove(sub));
        
        return sub;
    }

    public async Task Publish(PublishConfig publishConfig, AetherMessage message,
        CancellationToken cancellationToken)
    {
        var subjectKey = DefaultSubjectTypeMapper.From(publishConfig).Subject;
        message.Headers[MessageHeader.Subject] = subjectKey;
        
        var tasks = subscriptions.SelectMany(subs=>subs.Value).Select(
            async sub => { await sub.Writer.WriteAsync(new MessageContext(message), cancellationToken); });

        await Task.WhenAll(tasks);
    }

    public async Task<byte[]> Request(PublishConfig publishConfig, AetherMessage message,
        CancellationToken cancellationToken)
    {
        var subjectKey = DefaultSubjectTypeMapper.From(publishConfig).Subject;
        if (!subscriptions.TryGetValue(subjectKey, out var subscription))
            throw new InvalidOperationException("No handlers for this message type");

        var sub = subscription.First();

        var tcs = new TaskCompletionSource<byte[]>();
        var replyFunc = IsRequest(message.MessageType)
            ? new Func<byte[], CancellationToken, Task>(
                (response, _) =>
                {
                    tcs.TrySetResult(response);
                    return Task.CompletedTask;
                }
            )
            : null;

        await using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            await sub.Writer.WriteAsync(new MessageContext(message, replyFunc), cancellationToken);

            var timeoutTask = Task.Delay(requestTimeout, cancellationToken);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);

            if (completedTask == timeoutTask)
            {
                tcs.TrySetCanceled(cancellationToken);
                throw new TimeoutException("The request timed out.");
            }

            // tcs.Task is completed above
            return tcs.Task.Result;
        }
    }

    private static bool IsRequest(Type? type)
        => type?.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)) is true;
}

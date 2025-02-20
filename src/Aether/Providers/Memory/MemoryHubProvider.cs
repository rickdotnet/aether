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

        List<Task> tasks;
        if (subscriptions.TryGetValue(subjectKey, out var subscription))
        {
            tasks = subscription
                .Select(async sub => { await sub.Writer.WriteAsync(new MessageContext(message), cancellationToken); })
                .ToList();
        }
        else
        {
            tasks = subscriptions
                .Where(x => x.Key.Contains('*') || x.Key.EndsWith('>'))
                .SelectMany(subs => subs.Value).Select(
                    async sub => { await sub.Writer.WriteAsync(new MessageContext(message), cancellationToken); }
                ).ToList();

            if (tasks.Count == 0)
            {
                // no handlers for this message type?
                return;
            }
        }

        await Task.WhenAll(tasks);
    }

    public async Task<AetherData> Request(PublishConfig publishConfig, AetherMessage message, CancellationToken cancellationToken)
    {
        // TODO: will clean up exception handling
        if (!message.IsRequest)
            throw new InvalidOperationException("This method can only be used to send requests");

        var subjectKey = DefaultSubjectTypeMapper.From(publishConfig).Subject;
        message.Headers[MessageHeader.Subject] = subjectKey;

        if (!subscriptions.TryGetValue(subjectKey, out var subscription))
            throw new InvalidOperationException("No handlers for this message type");

        var sub = subscription.First();

        var tcs = new TaskCompletionSource<AetherData>();
        Func<AetherData, CancellationToken, Task> replyFunc = (response, _) =>
        {
            tcs.TrySetResult(response);
            return Task.CompletedTask;
        };

        await using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            await using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                var context = new MessageContext(message, replyFunc);
                await sub.Writer.WriteAsync(context, cancellationToken);

                if (!context.ReplyAvailable)
                    return AetherData.Empty;

                var timeoutTask = Task.Delay(requestTimeout, cancellationToken);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);

                if (completedTask != timeoutTask)
                    return tcs.Task.Result;

                tcs.TrySetCanceled(cancellationToken);
                throw new TimeoutException("The request timed out.");
            }
        }
    }
}

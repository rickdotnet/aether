using Aether.Abstractions.Messaging;
using Aether.Messaging;
using Aether.Messaging.Configuration;

namespace Aether.Abstractions.Providers;

public interface ISubscriptionProvider
{
    ISubscription AddSubscription(SubscriptionConfig config, Func<MessageContext, CancellationToken, Task> handler);
}

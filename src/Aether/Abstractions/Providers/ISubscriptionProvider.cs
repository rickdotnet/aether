using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;
using Aether.Messaging;

namespace Aether.Abstractions.Providers;

public interface ISubscriptionProvider
{
    ISubscription AddSubscription(SubscriptionConfig config, Func<MessageContext, CancellationToken, Task> handler);
}

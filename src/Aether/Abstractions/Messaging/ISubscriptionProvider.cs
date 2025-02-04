using Aether.Abstractions.Messaging.Configuration;

namespace Aether.Abstractions.Messaging;

public interface ISubscriptionProvider
{
    ISubscription AddSubscription(SubscriptionContext context);
}

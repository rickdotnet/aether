using Aether.Abstractions.Messaging;
using Aether.Abstractions.Messaging.Configuration;

namespace Aether.Abstractions.Providers;

public interface ISubscriptionProvider
{
    ISubscription AddSubscription(SubscriptionContext context);
}

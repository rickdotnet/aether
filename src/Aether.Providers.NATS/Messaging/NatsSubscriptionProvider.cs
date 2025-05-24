// using Aether.Abstractions.Messaging;
// using Aether.Abstractions.Messaging.Configuration;
// using Microsoft.Extensions.Logging;
// using NATS.Client.Core;
//
// namespace Aether.Providers.NATS.Messaging;
//
// public class NatsSubscriptionProvider : ISubscriptionProvider
// {
//     private readonly INatsConnection connection;
//     private readonly ILoggerFactory loggerFactory;
//
//     public NatsSubscriptionProvider(INatsConnection connection, ILoggerFactory loggerFactory)
//     {
//         this.connection = connection;
//         this.loggerFactory = loggerFactory;
//     }
//
//     public ISubscription AddSubscription(SubscriptionContext context)
//     {
//          
//         var subscriptionContext = new NatsSubscriptionContext(context);
//         return subscriptionContext.IsJetStream
//             ? new NatsJetStreamSubscription(
//                 connection,
//                 loggerFactory.CreateLogger<NatsJetStreamSubscription>(),
//                 subscriptionContext)
//             : new NatsCoreSubscription(
//                 connection,
//                 loggerFactory.CreateLogger<NatsCoreSubscription>(),
//                 subscriptionContext);
//     }
// }

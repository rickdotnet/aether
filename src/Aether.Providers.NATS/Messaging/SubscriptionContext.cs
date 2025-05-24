// using Aether.Abstractions.Messaging;
// using Aether.Abstractions.Messaging.Configuration;
// using Aether.Messaging;
// using NATS.Client.JetStream.Models;
//
// namespace Aether.Providers.NATS.Messaging;
//
// public class NatsSubscriptionContext
// {
//     private readonly SubscriptionContext context;
//     private const string ConsumerConfigKey = "nats-consumer-config";
//     
//     public SubjectTypeMapping SubjectMapping => context.SubjectMapping;
//     public EndpointConfig EndpointConfig => context.EndpointConfig;
//     public Func<MessageContext, CancellationToken, Task<AckSignal>> Handler => context.Handler;
//     public ConsumerConfig? ConsumerConfig { get; }
//
//     public bool IsJetStream => ConsumerConfig != null;
//     
//     public NatsSubscriptionContext(SubscriptionContext context)
//     {
//         this.context = context;
//         ConsumerConfig = EndpointConfig.ProviderConfig.GetValueOrDefault(ConsumerConfigKey) as ConsumerConfig;
//     }
// }
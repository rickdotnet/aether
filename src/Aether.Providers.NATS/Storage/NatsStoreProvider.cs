using Aether.Abstractions.Hosting;
using Aether.Abstractions.Storage;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using NATS.Net;

namespace Aether.Providers.NATS.Storage;

public sealed class NatsStoreProvider : IStoreProvider
{
    private readonly INatsKVContext kvContext;

    public NatsStoreProvider(INatsConnection nats, NatsJSOpts? defaultJsOpts)
    {
        defaultJsOpts ??= new NatsJSOpts(nats.Opts)
        {
            DefaultConsumeOpts = new NatsJSConsumeOpts()
            {
                // TODO: 1 MB as int
                MaxBytes = 1 * 1024 * 1024,

                MaxMsgs = 1_000
            }
        };
        
        var jsContext = new NatsJSContext(nats, defaultJsOpts with {});
        kvContext = jsContext.CreateKeyValueStoreContext();
    }

    public async Task<IStore> CreateStore(StorageRegistration registration,
        CancellationToken cancellationToken = default)
    {
        return new NatsKvStore(
            await kvContext.CreateStoreAsync(registration.StoreName, cancellationToken)
        );
    }
}

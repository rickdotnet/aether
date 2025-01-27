using Microsoft.Extensions.DependencyInjection;

namespace Aether.Abstractions.Hosting;

public interface IAetherBuilder
{
    IMessagingBuilder Messaging { get; }
    IStorageBuilder Storage { get; }

    void RegisterServices(Action<IServiceCollection> registerAction);
}

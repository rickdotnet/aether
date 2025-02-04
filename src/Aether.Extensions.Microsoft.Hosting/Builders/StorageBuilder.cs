using Aether.Abstractions.Hosting;
using Aether.Abstractions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Extensions.Microsoft.Hosting.Builders;

public class StorageBuilder : IStorageBuilder
{
    private readonly AetherBuilder aetherBuilder;

    public StorageBuilder(AetherBuilder aetherBuilder)
    {
        this.aetherBuilder = aetherBuilder;
    }

    public IStorageBuilder AddStore<T>() where T : IStorageProviderFactory
        => AddStore<T>(IDefaultStorageProvider.DefaultStoreName);

    public IStorageBuilder AddStore<T>(string storeName) where T : IStorageProviderFactory
        => AddStore(StorageRegistration.From<T>(storeName));

    public IStorageBuilder AddStore(StorageRegistration registration)
    {
        aetherBuilder.RegisterServices(services =>
        {
            services.AddSingleton(registration.ProviderFactoryType!);
            services
                .AddSingleton(registration);
        });
        return this;
    }
}
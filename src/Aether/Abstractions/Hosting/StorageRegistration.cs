using Aether.Abstractions.Storage;

namespace Aether.Abstractions.Hosting;

public sealed record StorageRegistration
{
    public string StoreName { get; }
    public Type? ProviderFactoryType { get; }
    public int? MaxBytes { get; init; }

    public StorageRegistration(string storeName, Type providerFactoryType)
    {
        StoreName = storeName;
        ProviderFactoryType = providerFactoryType;
    }

    public static StorageRegistration From<T>(string storeName) where T : IStoreProvider
        => new(storeName, typeof(T));
}
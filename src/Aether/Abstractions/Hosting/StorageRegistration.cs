using Aether.Abstractions.Storage;

namespace Aether.Abstractions.Hosting;

public sealed record StorageRegistration
{
    public string StoreName { get; }
    public Type? StoreType { get; }
     public int? MaxBytes { get; init; } // this is nats specific, will refactor later

    public StorageRegistration(string storeName, Type providerFactoryType)
    {
        StoreName = storeName;
        StoreType = providerFactoryType;
    }

    public static StorageRegistration From<T>(string storeName) where T : IStore
        => new(storeName, typeof(T));
}
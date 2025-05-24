using Aether.Abstractions.Storage;

namespace Aether.Abstractions.Hosting;

public sealed record StoreRegistration
{
    public string StoreName { get; }
    public Type? StoreType { get; }
     public int? MaxBytes { get; init; } // this is nats specific, will refactor later

    public StoreRegistration(string storeName, Type providerFactoryType)
    {
        StoreName = storeName;
        StoreType = providerFactoryType;
    }

    public static StoreRegistration From<T>(string storeName) where T : IStore
        => new(storeName, typeof(T));
}
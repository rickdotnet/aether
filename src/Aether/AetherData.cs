using RickDotNet.Base;
using RickDotNet.Extensions.Base;

namespace Aether;

public sealed class AetherData
{
    public static AetherData Empty { get; } = new(Array.Empty<byte>());
    public Memory<byte> Data { get; }
    public byte[] ToArray() => Data.ToArray();

    public AetherData(Memory<byte> data)
    {
        Data = data;
    }

    public static implicit operator AetherData(byte[] data) => new(data);
    public static implicit operator byte[](AetherData data) => data.ToArray();
    public static implicit operator Memory<byte>(AetherData data) => data.Data;
    public override string ToString() => System.Text.Encoding.UTF8.GetString(Data.Span);

    /// <summary>
    /// Attempts to deserialize the data into the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the data into.</typeparam>
    /// <returns>The deserialized data or default if the deserialization failed.</returns>
    public T? As<T>()
    {
        var result = Result.Try(() => System.Text.Json.JsonSerializer.Deserialize<T>(Data.Span));
        return result.ValueOrDefault();
    }

    public object? As(Type type)
    {
        var result = Result.Try(() => System.Text.Json.JsonSerializer.Deserialize(Data.Span, type));
        return result.ValueOrDefault();
    }

    /// <summary>
    /// Creates an AetherData instance from the specified data.
    /// </summary>
    /// <param name="data">The data to create the AetherData instance from.</param>
    /// <typeparam name="T">The type of the data.</typeparam>
    /// <returns>An AetherData instance created from the specified data.</returns>
    public static AetherData Serialize<T>(T data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        return new AetherData(System.Text.Encoding.UTF8.GetBytes(json));
    }

    public static AetherData<T> From<T>(AetherData data) => new(data);
}

public sealed class AetherData<T>
{
    public Type Type { get; }
    public AetherData Data { get; }
    
    public T? As() => Data.As<T>();

    internal AetherData(AetherData data)
    {
        Data = data;
        Type = typeof(T);
    }

    public static AetherData<T> Serialize(T data)
        => new(AetherData.Serialize(data));
}
namespace Aether.Abstractions.Messaging;

public interface IEndpointProvider
{
    object? GetService(Type endpointType);

    public T? GetService<T>();
}
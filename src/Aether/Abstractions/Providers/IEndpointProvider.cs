namespace Aether.Abstractions.Providers;

public interface IEndpointProvider
{
    object? GetService(Type endpointType);

    public T? GetService<T>();
}
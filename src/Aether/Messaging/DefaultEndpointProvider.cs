using Aether.Abstractions.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Messaging;

internal class DefaultEndpointProvider(IServiceProvider provider) : IEndpointProvider
{
    public object? GetService(Type endpointType) => provider.GetService(endpointType);
    public T? GetService<T>() => provider.GetService<T>();
}

/// <summary>
/// A generic endpoint provider that can be used to resolve instances of a given type.
/// </summary>
internal class GenericEndpointProvider : IEndpointProvider
{
    private readonly Dictionary<Type, object> services = new();

    public void AddService<T>(T service) where T : class
        => services[typeof(T)] = service!;

    public object? GetService(Type endpointType) 
        => services.GetValueOrDefault(endpointType);

    public T? GetService<T>() => services.TryGetValue(typeof(T), out var service) ? (T)service : default;
}
using Aether.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Aether.Extensions.Microsoft.Hosting.Messaging;

internal class HostedEndpointProvider(IServiceProvider provider) : IEndpointProvider
{
    public object? GetService(Type endpointType) => provider.GetService(endpointType);
    public T? GetService<T>() => provider.GetService<T>();
    
    // TODO: keyed services will be needed soon
}

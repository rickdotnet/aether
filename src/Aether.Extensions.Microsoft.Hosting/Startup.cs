using Microsoft.Extensions.DependencyInjection;

namespace Aether.Extensions.Microsoft.Hosting;

public static class Startup
{
    public static IServiceCollection AddAether(
        this IServiceCollection services,
        Action<IAetherBuilder>? builderAction
    )
    {
        var aetherBuilder = new AetherBuilder(services);
        builderAction?.Invoke(aetherBuilder);
        
        aetherBuilder.Build();
        
        return services;
    }
}

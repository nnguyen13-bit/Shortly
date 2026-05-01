using Microsoft.Extensions.DependencyInjection;

namespace Shortly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application services will be registered here as they are implemented
        return services;
    }
}

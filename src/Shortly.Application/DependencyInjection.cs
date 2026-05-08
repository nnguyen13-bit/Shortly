using Microsoft.Extensions.DependencyInjection;
using Shortly.Application.CustomDomains;
using Shortly.Application.Links;

namespace Shortly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LinkService>();
        services.AddScoped<RedirectService>();
        services.AddScoped<CustomDomainService>();

        return services;
    }
}

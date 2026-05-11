using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Application.Interfaces;
using Shortly.Infrastructure.Caching;
using Shortly.Infrastructure.CodeGeneration;
using Shortly.Infrastructure.Configuration;
using Shortly.Infrastructure.Health;
using Shortly.Infrastructure.Persistence;
using Shortly.Infrastructure.Events;
using Shortly.Infrastructure.Resilience;

namespace Shortly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.Configure<ServiceBusSettings>(configuration.GetSection(ServiceBusSettings.SectionName));

        services.AddResiliencePolicies();

        services.AddSingleton<MongoDbContext>();
        services.AddScoped<ILinkRepository, MongoLinkRepository>();
        services.AddScoped<ICustomDomainRepository, MongoCustomDomainRepository>();
        services.AddSingleton<IShortCodeGenerator, RangeBasedCodeGenerator>();

        var serviceBusConnectionString = configuration.GetSection(ServiceBusSettings.SectionName)
            .GetValue<string>(nameof(ServiceBusSettings.ConnectionString));

        if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();
        }
        else
        {
            services.AddSingleton<IEventPublisher, LoggingEventPublisher>();
        }

        services.AddHostedService<MongoDbIndexInitialiser>();

        services.AddHealthChecks()
            .AddCheck<MongoDbHealthCheck>("mongodb", tags: ["ready"]);

        // Redis cache — falls back to NoOp if not configured
        var redisConnectionString = configuration.GetSection(RedisSettings.SectionName)
            .GetValue<string>(nameof(RedisSettings.ConnectionString));

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "shortly:";
            });
            services.AddSingleton<IRedirectCache, RedisRedirectCache>();
        }
        else
        {
            services.AddSingleton<IRedirectCache, NoOpRedirectCache>();
        }

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Shortly.Infrastructure.Resilience;

public static class ResilienceKeys
{
    public const string MongoDb = "mongodb";
    public const string ServiceBus = "servicebus";
}

public static class ResilienceExtensions
{
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        // MongoDB: retry transient failures + circuit breaker for sustained outages
        services.AddResiliencePipeline(ResilienceKeys.MongoDb, (builder, context) =>
        {
            var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Resilience.MongoDB");

            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(200),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<MongoException>(ex => IsTransient(ex))
                        .Handle<TimeoutException>(),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            args.Outcome.Exception,
                            "MongoDB retry attempt {AttemptNumber} after {Delay}ms.",
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds);
                        return default;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    BreakDuration = TimeSpan.FromSeconds(15),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<MongoException>(ex => IsTransient(ex))
                        .Handle<TimeoutException>(),
                    OnOpened = args =>
                    {
                        logger.LogError(
                            args.Outcome.Exception,
                            "MongoDB circuit breaker OPENED for {BreakDuration}s.",
                            args.BreakDuration.TotalSeconds);
                        return default;
                    },
                    OnClosed = _ =>
                    {
                        logger.LogInformation("MongoDB circuit breaker CLOSED. Resuming normal operations.");
                        return default;
                    }
                })
                .AddTimeout(TimeSpan.FromSeconds(5));
        });

        // Service Bus: retry for transient failures
        services.AddResiliencePipeline(ResilienceKeys.ServiceBus, (builder, context) =>
        {
            var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Resilience.ServiceBus");

            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(500),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            args.Outcome.Exception,
                            "Service Bus retry attempt {AttemptNumber} after {Delay}ms.",
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds);
                        return default;
                    }
                })
                .AddTimeout(TimeSpan.FromSeconds(10));
        });

        return services;
    }

    private static bool IsTransient(MongoException ex)
    {
        // MongoConnectionException = network issues
        // MongoNotPrimaryException = replica set failover
        // MongoNodeIsRecoveringException = node recovering from failover
        return ex is MongoConnectionException
            or MongoNotPrimaryException
            or MongoNodeIsRecoveringException;
    }
}

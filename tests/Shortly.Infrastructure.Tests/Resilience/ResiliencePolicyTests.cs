using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Retry;
using Shortly.Infrastructure.Resilience;

namespace Shortly.Infrastructure.Tests.Resilience;

public sealed class ResiliencePolicyTests
{
    [Fact]
    public void MongoDbPipeline_IsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddResiliencePolicies();

        var provider = services.BuildServiceProvider();
        var pipelineProvider = provider.GetRequiredService<ResiliencePipelineProvider<string>>();

        var pipeline = pipelineProvider.GetPipeline(ResilienceKeys.MongoDb);

        Assert.NotNull(pipeline);
    }

    [Fact]
    public void ServiceBusPipeline_IsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddResiliencePolicies();

        var provider = services.BuildServiceProvider();
        var pipelineProvider = provider.GetRequiredService<ResiliencePipelineProvider<string>>();

        var pipeline = pipelineProvider.GetPipeline(ResilienceKeys.ServiceBus);

        Assert.NotNull(pipeline);
    }

    [Fact]
    public async Task MongoDbPipeline_RetriesOnTransientFailure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddResiliencePolicies();

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencePipelineProvider<string>>()
            .GetPipeline(ResilienceKeys.MongoDb);

        var attempts = 0;

        var result = await pipeline.ExecuteAsync(async ct =>
        {
            attempts++;
            if (attempts < 3)
                throw new MongoDB.Driver.MongoConnectionException(
                    new MongoDB.Driver.Core.Connections.ConnectionId(
                        new MongoDB.Driver.Core.Servers.ServerId(
                            new MongoDB.Driver.Core.Clusters.ClusterId(), new System.Net.DnsEndPoint("localhost", 27017))),
                    "Transient failure");
            return "success";
        });

        Assert.Equal("success", result);
        Assert.Equal(3, attempts); // 2 failures + 1 success
    }

    [Fact]
    public async Task MongoDbPipeline_DoesNotRetryNonTransientFailure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddResiliencePolicies();

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencePipelineProvider<string>>()
            .GetPipeline(ResilienceKeys.MongoDb);

        var attempts = 0;

        // MongoCommandException is not in our transient list
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await pipeline.ExecuteAsync<string>(async ct =>
            {
                attempts++;
                throw new InvalidOperationException("Not transient");
            });
        });

        Assert.Equal(1, attempts); // No retries
    }

    [Fact]
    public async Task ServiceBusPipeline_RetriesOnFailure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddResiliencePolicies();

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencePipelineProvider<string>>()
            .GetPipeline(ResilienceKeys.ServiceBus);

        var attempts = 0;

        var result = await pipeline.ExecuteAsync(async ct =>
        {
            attempts++;
            if (attempts < 2)
                throw new TimeoutException("Transient timeout");
            return "success";
        });

        Assert.Equal("success", result);
        Assert.Equal(2, attempts);
    }
}

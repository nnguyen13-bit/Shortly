using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shortly.Infrastructure.Caching;
using Testcontainers.Redis;

namespace Shortly.Infrastructure.Tests.Caching;

public sealed class RedisRedirectCacheTests : IAsyncLifetime
{
    private RedisContainer _container = null!;
    private RedisRedirectCache _cache = null!;

    public async Task InitializeAsync()
    {
        _container = new RedisBuilder("redis:7-alpine").Build();
        await _container.StartAsync();

        var redisOptions = Options.Create(new RedisCacheOptions
        {
            Configuration = _container.GetConnectionString(),
            InstanceName = "test:"
        });

        var distributedCache = new RedisCache(redisOptions);

        var settings = Options.Create(new RedisSettings
        {
            CacheTtlSeconds = 60
        });

        _cache = new RedisRedirectCache(
            distributedCache,
            NullLogger<RedisRedirectCache>.Instance,
            settings);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task GetDestinationUrl_CacheMiss_ReturnsNull()
    {
        var result = await _cache.GetDestinationUrlAsync("ab", "ABCDE");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAndGet_RoundTrips()
    {
        await _cache.SetDestinationUrlAsync("ab", "XYZAB", "https://example.com");

        var result = await _cache.GetDestinationUrlAsync("ab", "XYZAB");

        Assert.Equal("https://example.com", result);
    }

    [Fact]
    public async Task Evict_RemovesCachedEntry()
    {
        await _cache.SetDestinationUrlAsync("cd", "MNOPQ", "https://example.com");

        await _cache.EvictAsync("cd", "MNOPQ");

        var result = await _cache.GetDestinationUrlAsync("cd", "MNOPQ");
        Assert.Null(result);
    }

    [Fact]
    public async Task DifferentKeys_AreIndependent()
    {
        await _cache.SetDestinationUrlAsync("ab", "CODE1", "https://one.com");
        await _cache.SetDestinationUrlAsync("ab", "CODE2", "https://two.com");

        Assert.Equal("https://one.com", await _cache.GetDestinationUrlAsync("ab", "CODE1"));
        Assert.Equal("https://two.com", await _cache.GetDestinationUrlAsync("ab", "CODE2"));
    }

    [Fact]
    public async Task Evict_OnlyAffectsTargetKey()
    {
        await _cache.SetDestinationUrlAsync("ab", "AAAAA", "https://a.com");
        await _cache.SetDestinationUrlAsync("ab", "BBBBB", "https://b.com");

        await _cache.EvictAsync("ab", "AAAAA");

        Assert.Null(await _cache.GetDestinationUrlAsync("ab", "AAAAA"));
        Assert.Equal("https://b.com", await _cache.GetDestinationUrlAsync("ab", "BBBBB"));
    }
}

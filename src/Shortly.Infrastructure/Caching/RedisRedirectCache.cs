using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shortly.Application.Interfaces;

namespace Shortly.Infrastructure.Caching;

public sealed class RedisRedirectCache : IRedirectCache
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisRedirectCache> _logger;
    private readonly TimeSpan _ttl;

    public RedisRedirectCache(
        IDistributedCache cache,
        ILogger<RedisRedirectCache> logger,
        IOptions<RedisSettings> settings)
    {
        _cache = cache;
        _logger = logger;
        _ttl = TimeSpan.FromSeconds(settings.Value.CacheTtlSeconds);
    }

    public async Task<string?> GetDestinationUrlAsync(
        string domainPrefix,
        string shortCode,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(domainPrefix, shortCode);

        try
        {
            return await _cache.GetStringAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache read failed for key {CacheKey}. Falling through to database.", key);
            return null;
        }
    }

    public async Task SetDestinationUrlAsync(
        string domainPrefix,
        string shortCode,
        string destinationUrl,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(domainPrefix, shortCode);

        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _ttl
            };

            await _cache.SetStringAsync(key, destinationUrl, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache write failed for key {CacheKey}. Continuing without cache.", key);
        }
    }

    public async Task EvictAsync(
        string domainPrefix,
        string shortCode,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(domainPrefix, shortCode);

        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache eviction failed for key {CacheKey}.", key);
        }
    }

    private static string BuildKey(string domainPrefix, string shortCode) =>
        $"redirect:{domainPrefix}:{shortCode}";
}

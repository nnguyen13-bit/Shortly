using Shortly.Application.Interfaces;

namespace Shortly.Infrastructure.Caching;

/// <summary>
/// No-op cache implementation used when Redis is not configured.
/// Falls through to the database on every request.
/// </summary>
public sealed class NoOpRedirectCache : IRedirectCache
{
    public Task<string?> GetDestinationUrlAsync(string domainPrefix, string shortCode, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    public Task SetDestinationUrlAsync(string domainPrefix, string shortCode, string destinationUrl, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task EvictAsync(string domainPrefix, string shortCode, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

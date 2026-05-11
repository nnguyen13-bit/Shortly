namespace Shortly.Application.Interfaces;

public interface IRedirectCache
{
    Task<string?> GetDestinationUrlAsync(string domainPrefix, string shortCode, CancellationToken cancellationToken = default);
    Task SetDestinationUrlAsync(string domainPrefix, string shortCode, string destinationUrl, CancellationToken cancellationToken = default);
    Task EvictAsync(string domainPrefix, string shortCode, CancellationToken cancellationToken = default);
}

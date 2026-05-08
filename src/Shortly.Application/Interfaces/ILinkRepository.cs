using Shortly.Domain.LinkManagement;

namespace Shortly.Application.Interfaces;

public interface ILinkRepository
{
    Task<Link?> GetByIdAsync(LinkId id, CancellationToken cancellationToken = default);
    Task<Link?> GetByPrefixAndCodeAsync(DomainPrefix prefix, ShortCode code, CancellationToken cancellationToken = default);
    Task AddAsync(Link link, CancellationToken cancellationToken = default);
    Task UpdateAsync(Link link, CancellationToken cancellationToken = default);
    Task<long> CountByPrefixAsync(DomainPrefix prefix, CancellationToken cancellationToken = default);
}

using Shortly.Domain.CustomDomains;
using Shortly.Domain.LinkManagement;

namespace Shortly.Application.Interfaces;

public interface ICustomDomainRepository
{
    Task<CustomDomain?> GetByPrefixAsync(DomainPrefix prefix, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomDomain>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task AddAsync(CustomDomain customDomain, CancellationToken cancellationToken = default);
    Task UpdateAsync(CustomDomain customDomain, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(DomainPrefix prefix, CancellationToken cancellationToken = default);
}

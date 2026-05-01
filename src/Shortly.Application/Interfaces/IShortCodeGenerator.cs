using Shortly.Domain.LinkManagement;

namespace Shortly.Application.Interfaces;

public interface IShortCodeGenerator
{
    Task<ShortCode> GenerateAsync(DomainPrefix prefix, CancellationToken cancellationToken = default);
}

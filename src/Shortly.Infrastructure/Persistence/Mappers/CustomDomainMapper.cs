using Shortly.Domain.CustomDomains;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Persistence.Documents;

namespace Shortly.Infrastructure.Persistence.Mappers;

public static class CustomDomainMapper
{
    public static CustomDomainDocument ToDocument(CustomDomain domain)
    {
        return new CustomDomainDocument
        {
            Id = domain.Id.Value,
            Prefix = domain.Prefix.Value,
            Name = domain.Name,
            Description = domain.Description,
            IsActive = domain.IsActive,
            CreatedAt = domain.CreatedAt,
            Version = domain.Version
        };
    }

    public static CustomDomain ToDomain(CustomDomainDocument document)
    {
        return new CustomDomain(
            new CustomDomainId(document.Id),
            new DomainPrefix(document.Prefix),
            document.Name,
            document.Description,
            document.IsActive,
            document.CreatedAt,
            document.Version);
    }
}

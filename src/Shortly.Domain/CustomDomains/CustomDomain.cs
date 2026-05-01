using Shortly.Domain.Common;
using Shortly.Domain.LinkManagement;

namespace Shortly.Domain.CustomDomains;

public sealed class CustomDomain : Entity<CustomDomainId>
{
    public DomainPrefix Prefix { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private CustomDomain() { } // Mapping

    public static CustomDomain Register(DomainPrefix prefix, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Domain name cannot be empty.", nameof(name));

        var domain = new CustomDomain
        {
            Id = CustomDomainId.New(),
            Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix)),
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        domain.IncrementVersion();
        domain.RaiseDomainEvent(new CustomDomainRegisteredEvent(domain.Id, domain.Prefix, domain.Name));
        return domain;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return; // Idempotent

        IsActive = false;
        IncrementVersion();
        RaiseDomainEvent(new CustomDomainDeactivatedEvent(Id, Prefix));
    }
}

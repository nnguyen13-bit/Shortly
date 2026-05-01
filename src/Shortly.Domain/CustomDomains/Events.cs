using Shortly.Domain.Common;
using Shortly.Domain.LinkManagement;

namespace Shortly.Domain.CustomDomains;

public sealed class CustomDomainRegisteredEvent : DomainEvent
{
    public CustomDomainId CustomDomainId { get; }
    public DomainPrefix Prefix { get; }
    public string Name { get; }

    public CustomDomainRegisteredEvent(CustomDomainId customDomainId, DomainPrefix prefix, string name)
    {
        CustomDomainId = customDomainId;
        Prefix = prefix;
        Name = name;
    }
}

public sealed class CustomDomainDeactivatedEvent : DomainEvent
{
    public CustomDomainId CustomDomainId { get; }
    public DomainPrefix Prefix { get; }

    public CustomDomainDeactivatedEvent(CustomDomainId customDomainId, DomainPrefix prefix)
    {
        CustomDomainId = customDomainId;
        Prefix = prefix;
    }
}

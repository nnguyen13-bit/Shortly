using Shortly.Domain.Common;

namespace Shortly.Domain.LinkManagement;

public sealed class LinkCreatedEvent : DomainEvent
{
    public LinkId LinkId { get; }
    public DomainPrefix DomainPrefix { get; }
    public ShortCode ShortCode { get; }
    public DestinationUrl DestinationUrl { get; }

    public LinkCreatedEvent(LinkId linkId, DomainPrefix domainPrefix, ShortCode shortCode, DestinationUrl destinationUrl)
    {
        LinkId = linkId;
        DomainPrefix = domainPrefix;
        ShortCode = shortCode;
        DestinationUrl = destinationUrl;
    }
}

public sealed class LinkDisabledEvent : DomainEvent
{
    public LinkId LinkId { get; }
    public DomainPrefix DomainPrefix { get; }
    public ShortCode ShortCode { get; }

    public LinkDisabledEvent(LinkId linkId, DomainPrefix domainPrefix, ShortCode shortCode)
    {
        LinkId = linkId;
        DomainPrefix = domainPrefix;
        ShortCode = shortCode;
    }
}

public sealed class LinkRedirectedEvent : DomainEvent
{
    public LinkId LinkId { get; }
    public DomainPrefix DomainPrefix { get; }
    public ShortCode ShortCode { get; }

    public LinkRedirectedEvent(LinkId linkId, DomainPrefix domainPrefix, ShortCode shortCode)
    {
        LinkId = linkId;
        DomainPrefix = domainPrefix;
        ShortCode = shortCode;
    }
}

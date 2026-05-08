using Shortly.Domain.Common;

namespace Shortly.Domain.LinkManagement;

public sealed class Link : Entity<LinkId>
{
    public ShortCode ShortCode { get; private set; } = null!;
    public DomainPrefix DomainPrefix { get; private set; } = null!;
    public DestinationUrl DestinationUrl { get; private set; } = null!;
    public LinkStatus Status { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public LinkMetadata Metadata { get; private set; } = null!;

    private Link() { } // EF/MongoDB mapping

    /// <summary>
    /// Reconstitutes a Link from persistence. No validation, no domain events.
    /// </summary>
    internal Link(
        LinkId id,
        ShortCode shortCode,
        DomainPrefix domainPrefix,
        DestinationUrl destinationUrl,
        LinkStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt,
        string createdBy,
        LinkMetadata metadata,
        int version)
    {
        Id = id;
        ShortCode = shortCode;
        DomainPrefix = domainPrefix;
        DestinationUrl = destinationUrl;
        Status = status;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        CreatedBy = createdBy;
        Metadata = metadata;
        SetVersion(version);
    }

    public static Link Create(
        ShortCode shortCode,
        DomainPrefix domainPrefix,
        DestinationUrl destinationUrl,
        string createdBy,
        DateTimeOffset? expiresAt = null,
        LinkMetadata? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy cannot be empty.", nameof(createdBy));

        if (expiresAt.HasValue && expiresAt.Value <= DateTimeOffset.UtcNow)
            throw new LinkDomainException("Expiry date cannot be in the past.");

        var link = new Link
        {
            Id = LinkId.New(),
            ShortCode = shortCode ?? throw new ArgumentNullException(nameof(shortCode)),
            DomainPrefix = domainPrefix ?? throw new ArgumentNullException(nameof(domainPrefix)),
            DestinationUrl = destinationUrl ?? throw new ArgumentNullException(nameof(destinationUrl)),
            Status = LinkStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            CreatedBy = createdBy,
            Metadata = metadata ?? new LinkMetadata()
        };

        link.IncrementVersion();
        link.RaiseDomainEvent(new LinkCreatedEvent(link.Id, link.DomainPrefix, link.ShortCode, link.DestinationUrl));
        return link;
    }

    public void Disable()
    {
        if (Status == LinkStatus.Expired)
            throw new LinkDomainException("Cannot disable an expired link.");

        if (Status == LinkStatus.Disabled)
            return; // Idempotent

        Status = LinkStatus.Disabled;
        IncrementVersion();
        RaiseDomainEvent(new LinkDisabledEvent(Id, DomainPrefix, ShortCode));
    }

    public bool IsExpired()
    {
        return Status == LinkStatus.Expired ||
               (ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow);
    }

    public bool IsRedirectable()
    {
        return Status == LinkStatus.Active && !IsExpired();
    }
}

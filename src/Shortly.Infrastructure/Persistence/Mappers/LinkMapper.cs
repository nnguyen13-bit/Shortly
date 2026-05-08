using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Persistence.Documents;

namespace Shortly.Infrastructure.Persistence.Mappers;

internal static class LinkMapper
{
    public static LinkDocument ToDocument(Link link)
    {
        return new LinkDocument
        {
            Id = link.Id.Value,
            ShortCode = link.ShortCode.Value,
            DomainPrefix = link.DomainPrefix.Value,
            DestinationUrl = link.DestinationUrl.Value,
            Status = link.Status.ToString(),
            CreatedAt = link.CreatedAt,
            ExpiresAt = link.ExpiresAt,
            CreatedBy = link.CreatedBy,
            Metadata = link.Metadata.Tags.Count > 0
                ? new Dictionary<string, string>(link.Metadata.Tags)
                : null,
            Version = link.Version
        };
    }

    public static Link ToDomain(LinkDocument document)
    {
        return new Link(
            new LinkId(document.Id),
            new ShortCode(document.ShortCode),
            new DomainPrefix(document.DomainPrefix),
            new DestinationUrl(document.DestinationUrl),
            Enum.Parse<LinkStatus>(document.Status),
            document.CreatedAt,
            document.ExpiresAt,
            document.CreatedBy,
            new LinkMetadata(document.Metadata),
            document.Version);
    }
}

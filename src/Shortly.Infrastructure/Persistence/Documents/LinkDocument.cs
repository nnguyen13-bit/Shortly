using MongoDB.Bson.Serialization.Attributes;
using Shortly.Domain.LinkManagement;

namespace Shortly.Infrastructure.Persistence.Documents;

public sealed class LinkDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public string ShortCode { get; set; } = null!;
    public string DomainPrefix { get; set; } = null!;
    public string DestinationUrl { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public Dictionary<string, string>? Metadata { get; set; }
    public int Version { get; set; }

    public static LinkDocument FromDomain(Link link)
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

    public Link ToDomain()
    {
        // Reconstitute the domain entity via reflection to bypass factory validation
        // (document already passed validation on creation)
        var link = (Link)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Link));

        // Use reflection to set private properties
        var type = typeof(Link);
        SetProperty(link, "Id", new LinkId(Id));
        SetProperty(link, "ShortCode", new ShortCode(ShortCode));
        SetProperty(link, "DomainPrefix", new Domain.LinkManagement.DomainPrefix(DomainPrefix));
        SetProperty(link, "DestinationUrl", new DestinationUrl(DestinationUrl));
        SetProperty(link, "Status", Enum.Parse<LinkStatus>(Status));
        SetProperty(link, "CreatedAt", CreatedAt);
        SetProperty(link, "ExpiresAt", ExpiresAt);
        SetProperty(link, "CreatedBy", CreatedBy);
        SetProperty(link, "Metadata", new LinkMetadata(Metadata));

        return link;
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var backingField = obj.GetType().GetField($"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(obj, value);
    }
}

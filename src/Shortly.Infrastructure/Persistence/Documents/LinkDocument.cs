using MongoDB.Bson.Serialization.Attributes;

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
}

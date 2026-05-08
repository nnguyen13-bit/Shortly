using MongoDB.Bson.Serialization.Attributes;

namespace Shortly.Infrastructure.Persistence.Documents;

public sealed class CustomDomainDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public string Prefix { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }
}

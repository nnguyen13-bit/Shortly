using MongoDB.Bson.Serialization.Attributes;
using Shortly.Domain.CustomDomains;
using Shortly.Domain.LinkManagement;

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

    public static CustomDomainDocument FromDomain(CustomDomain customDomain)
    {
        return new CustomDomainDocument
        {
            Id = customDomain.Id.Value,
            Prefix = customDomain.Prefix.Value,
            Name = customDomain.Name,
            Description = customDomain.Description,
            IsActive = customDomain.IsActive,
            CreatedAt = customDomain.CreatedAt,
            Version = customDomain.Version
        };
    }

    public CustomDomain ToDomain()
    {
        var customDomain = (CustomDomain)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(CustomDomain));

        SetProperty(customDomain, "Id", new CustomDomainId(Id));
        SetProperty(customDomain, "Prefix", new DomainPrefix(Prefix));
        SetProperty(customDomain, "Name", Name);
        SetProperty(customDomain, "Description", Description);
        SetProperty(customDomain, "IsActive", IsActive);
        SetProperty(customDomain, "CreatedAt", CreatedAt);

        return customDomain;
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        var backingField = obj.GetType().GetField($"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(obj, value);
    }
}

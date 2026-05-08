using Shortly.Domain.CustomDomains;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Persistence.Documents;
using Shortly.Infrastructure.Persistence.Mappers;

namespace Shortly.Infrastructure.Tests.Persistence;

public class CustomDomainMapperTests
{
    [Fact]
    public void ToDocument_MapsAllProperties()
    {
        var domain = CustomDomain.Register(
            new DomainPrefix("app1"),
            "My Application",
            "Primary app domain");

        var document = CustomDomainMapper.ToDocument(domain);

        Assert.Equal(domain.Id.Value, document.Id);
        Assert.Equal("app1", document.Prefix);
        Assert.Equal("My Application", document.Name);
        Assert.Equal("Primary app domain", document.Description);
        Assert.True(document.IsActive);
        Assert.Equal(domain.CreatedAt, document.CreatedAt);
        Assert.Equal(domain.Version, document.Version);
    }

    [Fact]
    public void ToDocument_NullDescription_MapsNull()
    {
        var domain = CustomDomain.Register(
            new DomainPrefix("nd"),
            "No Description");

        var document = CustomDomainMapper.ToDocument(domain);

        Assert.Null(document.Description);
    }

    [Fact]
    public void ToDomain_MapsAllProperties()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-5);

        var document = new CustomDomainDocument
        {
            Id = id,
            Prefix = "shop",
            Name = "Shop Domain",
            Description = "E-commerce links",
            IsActive = true,
            CreatedAt = createdAt,
            Version = 2
        };

        var domain = CustomDomainMapper.ToDomain(document);

        Assert.Equal(id, domain.Id.Value);
        Assert.Equal("shop", domain.Prefix.Value);
        Assert.Equal("Shop Domain", domain.Name);
        Assert.Equal("E-commerce links", domain.Description);
        Assert.True(domain.IsActive);
        Assert.Equal(createdAt, domain.CreatedAt);
        Assert.Equal(2, domain.Version);
    }

    [Fact]
    public void ToDomain_InactiveDomain_MapsCorrectly()
    {
        var document = new CustomDomainDocument
        {
            Id = Guid.NewGuid(),
            Prefix = "ol",
            Name = "Old Domain",
            Description = null,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            Version = 3
        };

        var domain = CustomDomainMapper.ToDomain(document);

        Assert.False(domain.IsActive);
    }

    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        var domain = CustomDomain.Register(
            new DomainPrefix("rt"),
            "RoundTrip Domain",
            "Testing round trip");

        var document = CustomDomainMapper.ToDocument(domain);
        var reconstituted = CustomDomainMapper.ToDomain(document);

        Assert.Equal(domain.Id, reconstituted.Id);
        Assert.Equal(domain.Prefix, reconstituted.Prefix);
        Assert.Equal(domain.Name, reconstituted.Name);
        Assert.Equal(domain.Description, reconstituted.Description);
        Assert.Equal(domain.IsActive, reconstituted.IsActive);
        Assert.Equal(domain.CreatedAt, reconstituted.CreatedAt);
        Assert.Equal(domain.Version, reconstituted.Version);
    }

    [Fact]
    public void RoundTrip_NoEvents_OnReconstitutedEntity()
    {
        var domain = CustomDomain.Register(
            new DomainPrefix("ev"),
            "Event Test");

        var document = CustomDomainMapper.ToDocument(domain);
        var reconstituted = CustomDomainMapper.ToDomain(document);

        Assert.Empty(reconstituted.DomainEvents);
    }
}

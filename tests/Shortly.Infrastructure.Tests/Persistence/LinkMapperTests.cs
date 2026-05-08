using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Persistence.Documents;
using Shortly.Infrastructure.Persistence.Mappers;

namespace Shortly.Infrastructure.Tests.Persistence;

public class LinkMapperTests
{
    [Fact]
    public void ToDocument_MapsAllProperties()
    {
        var link = Link.Create(
            new ShortCode("abc123"),
            new DomainPrefix("sh"),
            new DestinationUrl("https://example.com"),
            "user@test.com",
            expiresAt: DateTimeOffset.UtcNow.AddDays(7),
            metadata: new LinkMetadata(new Dictionary<string, string> { ["campaign"] = "launch" }));

        var document = LinkMapper.ToDocument(link);

        Assert.Equal(link.Id.Value, document.Id);
        Assert.Equal("abc123", document.ShortCode);
        Assert.Equal("sh", document.DomainPrefix);
        Assert.Equal("https://example.com", document.DestinationUrl);
        Assert.Equal("Active", document.Status);
        Assert.Equal(link.CreatedAt, document.CreatedAt);
        Assert.Equal(link.ExpiresAt, document.ExpiresAt);
        Assert.Equal("user@test.com", document.CreatedBy);
        Assert.NotNull(document.Metadata);
        Assert.Equal("launch", document.Metadata["campaign"]);
        Assert.Equal(link.Version, document.Version);
    }

    [Fact]
    public void ToDocument_NullMetadata_WhenNoTags()
    {
        var link = Link.Create(
            new ShortCode("xyz789"),
            new DomainPrefix("go"),
            new DestinationUrl("https://example.com/page"),
            "admin");

        var document = LinkMapper.ToDocument(link);

        Assert.Null(document.Metadata);
    }

    [Fact]
    public void ToDomain_MapsAllProperties()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-1);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        var document = new LinkDocument
        {
            Id = id,
            ShortCode = "test01",
            DomainPrefix = "lnk",
            DestinationUrl = "https://target.io/path",
            Status = "Active",
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            CreatedBy = "tester",
            Metadata = new Dictionary<string, string> { ["env"] = "prod" },
            Version = 3
        };

        var link = LinkMapper.ToDomain(document);

        Assert.Equal(id, link.Id.Value);
        Assert.Equal("test01", link.ShortCode.Value);
        Assert.Equal("lnk", link.DomainPrefix.Value);
        Assert.Equal("https://target.io/path", link.DestinationUrl.Value);
        Assert.Equal(LinkStatus.Active, link.Status);
        Assert.Equal(createdAt, link.CreatedAt);
        Assert.Equal(expiresAt, link.ExpiresAt);
        Assert.Equal("tester", link.CreatedBy);
        Assert.Equal("prod", link.Metadata.Tags["env"]);
        Assert.Equal(3, link.Version);
    }

    [Fact]
    public void ToDomain_NullMetadata_CreatesEmptyTags()
    {
        var document = new LinkDocument
        {
            Id = Guid.NewGuid(),
            ShortCode = "nomda",
            DomainPrefix = "sn",
            DestinationUrl = "https://example.com",
            Status = "Active",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = null,
            CreatedBy = "user",
            Metadata = null,
            Version = 1
        };

        var link = LinkMapper.ToDomain(document);

        Assert.Empty(link.Metadata.Tags);
    }

    [Fact]
    public void ToDomain_DisabledStatus_MapsCorrectly()
    {
        var document = new LinkDocument
        {
            Id = Guid.NewGuid(),
            ShortCode = "dis01",
            DomainPrefix = "xx",
            DestinationUrl = "https://old.com",
            Status = "Disabled",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
            ExpiresAt = null,
            CreatedBy = "admin",
            Metadata = null,
            Version = 2
        };

        var link = LinkMapper.ToDomain(document);

        Assert.Equal(LinkStatus.Disabled, link.Status);
    }

    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        var link = Link.Create(
            new ShortCode("round1"),
            new DomainPrefix("go"),
            new DestinationUrl("https://round.trip/test"),
            "rounduser",
            expiresAt: DateTimeOffset.UtcNow.AddDays(14),
            metadata: new LinkMetadata(new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" }));

        var document = LinkMapper.ToDocument(link);
        var reconstituted = LinkMapper.ToDomain(document);

        Assert.Equal(link.Id, reconstituted.Id);
        Assert.Equal(link.ShortCode, reconstituted.ShortCode);
        Assert.Equal(link.DomainPrefix, reconstituted.DomainPrefix);
        Assert.Equal(link.DestinationUrl, reconstituted.DestinationUrl);
        Assert.Equal(link.Status, reconstituted.Status);
        Assert.Equal(link.CreatedAt, reconstituted.CreatedAt);
        Assert.Equal(link.ExpiresAt, reconstituted.ExpiresAt);
        Assert.Equal(link.CreatedBy, reconstituted.CreatedBy);
        Assert.Equal(link.Metadata, reconstituted.Metadata);
        Assert.Equal(link.Version, reconstituted.Version);
    }

    [Fact]
    public void RoundTrip_NoEvents_OnReconstitutedEntity()
    {
        var link = Link.Create(
            new ShortCode("evt01"),
            new DomainPrefix("ev"),
            new DestinationUrl("https://events.test"),
            "evtuser");

        var document = LinkMapper.ToDocument(link);
        var reconstituted = LinkMapper.ToDomain(document);

        Assert.Empty(reconstituted.DomainEvents);
    }
}

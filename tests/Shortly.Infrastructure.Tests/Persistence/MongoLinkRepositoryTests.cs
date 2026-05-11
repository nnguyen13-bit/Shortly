using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Persistence;

namespace Shortly.Infrastructure.Tests.Persistence;

[Collection("MongoDB")]
public sealed class MongoLinkRepositoryTests
{
    private readonly MongoLinkRepository _repository;

    public MongoLinkRepositoryTests(MongoDbFixture fixture)
    {
        _repository = new MongoLinkRepository(fixture.Context, TestResiliencePipeline.Provider);
    }

    // --- AddAsync + GetByIdAsync ---

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsLink()
    {
        var link = CreateTestLink();

        await _repository.AddAsync(link);
        var retrieved = await _repository.GetByIdAsync(link.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(link.Id, retrieved.Id);
        Assert.Equal(link.ShortCode, retrieved.ShortCode);
        Assert.Equal(link.DomainPrefix, retrieved.DomainPrefix);
        Assert.Equal(link.DestinationUrl, retrieved.DestinationUrl);
        Assert.Equal(link.Status, retrieved.Status);
        Assert.Equal(link.CreatedBy, retrieved.CreatedBy);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var nonExistentId = LinkId.New();

        var result = await _repository.GetByIdAsync(nonExistentId);

        Assert.Null(result);
    }

    // --- GetByPrefixAndCodeAsync ---

    [Fact]
    public async Task GetByPrefixAndCodeAsync_ExistingLink_ReturnsLink()
    {
        var link = CreateTestLink();
        await _repository.AddAsync(link);

        var retrieved = await _repository.GetByPrefixAndCodeAsync(link.DomainPrefix, link.ShortCode);

        Assert.NotNull(retrieved);
        Assert.Equal(link.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetByPrefixAndCodeAsync_WrongPrefix_ReturnsNull()
    {
        var link = CreateTestLink();
        await _repository.AddAsync(link);

        var result = await _repository.GetByPrefixAndCodeAsync(new DomainPrefix("zz"), link.ShortCode);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPrefixAndCodeAsync_WrongCode_ReturnsNull()
    {
        var link = CreateTestLink();
        await _repository.AddAsync(link);

        var result = await _repository.GetByPrefixAndCodeAsync(link.DomainPrefix, new ShortCode("zzzzz"));

        Assert.Null(result);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_DisabledLink_PersistsNewStatus()
    {
        var link = CreateTestLink();
        await _repository.AddAsync(link);

        link.Disable();
        await _repository.UpdateAsync(link);

        var retrieved = await _repository.GetByIdAsync(link.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(LinkStatus.Disabled, retrieved.Status);
    }

    // --- CountByPrefixAsync ---

    [Fact]
    public async Task CountByPrefixAsync_ActiveLinks_ReturnsCorrectCount()
    {
        var prefix = new DomainPrefix(GenerateUniquePrefix());

        var link1 = CreateTestLink(prefix: prefix);
        var link2 = CreateTestLink(prefix: prefix);
        await _repository.AddAsync(link1);
        await _repository.AddAsync(link2);

        var count = await _repository.CountByPrefixAsync(prefix);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountByPrefixAsync_DisabledLinksExcluded()
    {
        var prefix = new DomainPrefix(GenerateUniquePrefix());

        var link1 = CreateTestLink(prefix: prefix);
        var link2 = CreateTestLink(prefix: prefix);
        await _repository.AddAsync(link1);
        await _repository.AddAsync(link2);

        link2.Disable();
        await _repository.UpdateAsync(link2);

        var count = await _repository.CountByPrefixAsync(prefix);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CountByPrefixAsync_DifferentPrefix_ReturnsZero()
    {
        var prefix = new DomainPrefix(GenerateUniquePrefix());
        var otherPrefix = new DomainPrefix(GenerateUniquePrefix());

        var link = CreateTestLink(prefix: prefix);
        await _repository.AddAsync(link);

        var count = await _repository.CountByPrefixAsync(otherPrefix);

        Assert.Equal(0, count);
    }

    // --- Metadata persistence ---

    [Fact]
    public async Task AddAsync_WithMetadata_PersistsAndRetrievesTags()
    {
        var metadata = new LinkMetadata(new Dictionary<string, string>
        {
            ["team"] = "platform",
            ["env"] = "production"
        });

        var link = CreateTestLink(metadata: metadata);
        await _repository.AddAsync(link);

        var retrieved = await _repository.GetByIdAsync(link.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.Metadata.Tags.Count);
        Assert.Equal("platform", retrieved.Metadata.Tags["team"]);
        Assert.Equal("production", retrieved.Metadata.Tags["env"]);
    }

    [Fact]
    public async Task AddAsync_WithExpiry_PersistsExpiresAt()
    {
        var expiry = DateTimeOffset.UtcNow.AddDays(7);
        var link = CreateTestLink(expiresAt: expiry);
        await _repository.AddAsync(link);

        var retrieved = await _repository.GetByIdAsync(link.Id);

        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.ExpiresAt);
        // Allow 1 second tolerance for serialisation rounding
        Assert.InRange(retrieved.ExpiresAt!.Value, expiry.AddSeconds(-1), expiry.AddSeconds(1));
    }

    // --- Helpers ---

    private static Link CreateTestLink(
        DomainPrefix? prefix = null,
        DateTimeOffset? expiresAt = null,
        LinkMetadata? metadata = null)
    {
        return Link.Create(
            new ShortCode(GenerateUniqueCode()),
            prefix ?? new DomainPrefix(GenerateUniquePrefix()),
            new DestinationUrl("https://example.com/test"),
            "integration-test",
            expiresAt,
            metadata);
    }

    private static string GenerateUniqueCode()
    {
        // 5-8 char Base62 code
        return "t" + Guid.NewGuid().ToString("N")[..5];
    }

    private static string GenerateUniquePrefix()
    {
        // 2-4 char lowercase prefix
        return "t" + Guid.NewGuid().ToString("N")[..2];
    }
}

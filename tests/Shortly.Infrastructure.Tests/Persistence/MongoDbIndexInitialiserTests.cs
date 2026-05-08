using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Shortly.Infrastructure.Persistence;
using Shortly.Infrastructure.Persistence.Documents;

namespace Shortly.Infrastructure.Tests.Persistence;

[Collection("MongoDB")]
public sealed class MongoDbIndexInitialiserTests
{
    private readonly MongoDbFixture _fixture;

    public MongoDbIndexInitialiserTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartAsync_CreatesLinkIndexes()
    {
        var initialiser = new MongoDbIndexInitialiser(
            _fixture.Context,
            NullLogger<MongoDbIndexInitialiser>.Instance);

        await initialiser.StartAsync(CancellationToken.None);

        var links = _fixture.Context.GetCollection<LinkDocument>("links");
        var indexes = await (await links.Indexes.ListAsync()).ToListAsync();
        var indexNames = indexes.Select(i => i["name"].AsString).ToList();

        Assert.Contains("ix_links_prefix_code_unique", indexNames);
        Assert.Contains("ix_links_expiresAt_ttl", indexNames);
        Assert.Contains("ix_links_status", indexNames);
    }

    [Fact]
    public async Task StartAsync_CreatesDomainIndexes()
    {
        var initialiser = new MongoDbIndexInitialiser(
            _fixture.Context,
            NullLogger<MongoDbIndexInitialiser>.Instance);

        await initialiser.StartAsync(CancellationToken.None);

        var domains = _fixture.Context.GetCollection<CustomDomainDocument>("domains");
        var indexes = await (await domains.Indexes.ListAsync()).ToListAsync();
        var indexNames = indexes.Select(i => i["name"].AsString).ToList();

        Assert.Contains("ix_domains_prefix_unique", indexNames);
    }

    [Fact]
    public async Task StartAsync_CalledTwice_IsIdempotent()
    {
        var initialiser = new MongoDbIndexInitialiser(
            _fixture.Context,
            NullLogger<MongoDbIndexInitialiser>.Instance);

        await initialiser.StartAsync(CancellationToken.None);
        await initialiser.StartAsync(CancellationToken.None); // Should not throw

        var links = _fixture.Context.GetCollection<LinkDocument>("links");
        var indexes = await (await links.Indexes.ListAsync()).ToListAsync();

        // _id index + 3 custom indexes
        Assert.Equal(4, indexes.Count);
    }

    [Fact]
    public async Task StartAsync_UniqueIndex_PreventsDuplicatePrefixCode()
    {
        var initialiser = new MongoDbIndexInitialiser(
            _fixture.Context,
            NullLogger<MongoDbIndexInitialiser>.Instance);
        await initialiser.StartAsync(CancellationToken.None);

        var links = _fixture.Context.GetCollection<LinkDocument>("links");

        var doc1 = new LinkDocument
        {
            Id = Guid.NewGuid(),
            DomainPrefix = "xx",
            ShortCode = "UniQ1",
            DestinationUrl = "https://example.com/1",
            Status = "Active",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            Version = 1
        };

        var doc2 = new LinkDocument
        {
            Id = Guid.NewGuid(),
            DomainPrefix = "xx",
            ShortCode = "UniQ1", // Same prefix + code
            DestinationUrl = "https://example.com/2",
            Status = "Active",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            Version = 1
        };

        await links.InsertOneAsync(doc1);

        await Assert.ThrowsAsync<MongoWriteException>(async () =>
            await links.InsertOneAsync(doc2));
    }
}

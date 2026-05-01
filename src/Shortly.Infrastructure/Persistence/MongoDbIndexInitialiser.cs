using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Shortly.Infrastructure.Configuration;
using Shortly.Infrastructure.Persistence;
using Shortly.Infrastructure.Persistence.Documents;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Shortly.Infrastructure.Persistence;

public sealed class MongoDbIndexInitialiser : IHostedService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<MongoDbIndexInitialiser> _logger;

    public MongoDbIndexInitialiser(MongoDbContext context, ILogger<MongoDbIndexInitialiser> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating MongoDB indexes...");

        await CreateLinkIndexes(cancellationToken);
        await CreateDomainIndexes(cancellationToken);

        _logger.LogInformation("MongoDB indexes created successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CreateLinkIndexes(CancellationToken cancellationToken)
    {
        var links = _context.GetCollection<LinkDocument>("links");

        var uniqueCompound = new CreateIndexModel<LinkDocument>(
            Builders<LinkDocument>.IndexKeys
                .Ascending(d => d.DomainPrefix)
                .Ascending(d => d.ShortCode),
            new CreateIndexOptions { Unique = true, Name = "ix_links_prefix_code_unique" });

        var ttlIndex = new CreateIndexModel<LinkDocument>(
            Builders<LinkDocument>.IndexKeys.Ascending(d => d.ExpiresAt),
            new CreateIndexOptions
            {
                Name = "ix_links_expiresAt_ttl",
                ExpireAfter = TimeSpan.Zero // Documents expire at the ExpiresAt time
            });

        var statusIndex = new CreateIndexModel<LinkDocument>(
            Builders<LinkDocument>.IndexKeys.Ascending(d => d.Status),
            new CreateIndexOptions { Name = "ix_links_status" });

        await links.Indexes.CreateManyAsync(
            [uniqueCompound, ttlIndex, statusIndex],
            cancellationToken);
    }

    private async Task CreateDomainIndexes(CancellationToken cancellationToken)
    {
        var domains = _context.GetCollection<CustomDomainDocument>("domains");

        var uniquePrefix = new CreateIndexModel<CustomDomainDocument>(
            Builders<CustomDomainDocument>.IndexKeys.Ascending(d => d.Prefix),
            new CreateIndexOptions { Unique = true, Name = "ix_domains_prefix_unique" });

        await domains.Indexes.CreateManyAsync([uniquePrefix], cancellationToken);
    }
}

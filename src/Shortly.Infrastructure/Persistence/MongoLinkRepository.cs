using MongoDB.Driver;
using Shortly.Application.Interfaces;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Persistence.Documents;

namespace Shortly.Infrastructure.Persistence;

public sealed class MongoLinkRepository : ILinkRepository
{
    private readonly IMongoCollection<LinkDocument> _collection;

    public MongoLinkRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<LinkDocument>("links");
    }

    public async Task<Link?> GetByIdAsync(LinkId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LinkDocument>.Filter.Eq(d => d.Id, id.Value);
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<Link?> GetByPrefixAndCodeAsync(DomainPrefix prefix, ShortCode code, CancellationToken cancellationToken = default)
    {
        var filter = Builders<LinkDocument>.Filter.And(
            Builders<LinkDocument>.Filter.Eq(d => d.DomainPrefix, prefix.Value),
            Builders<LinkDocument>.Filter.Eq(d => d.ShortCode, code.Value));

        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task AddAsync(Link link, CancellationToken cancellationToken = default)
    {
        var document = LinkDocument.FromDomain(link);
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Link link, CancellationToken cancellationToken = default)
    {
        var document = LinkDocument.FromDomain(link);
        var filter = Builders<LinkDocument>.Filter.Eq(d => d.Id, link.Id.Value);
        await _collection.ReplaceOneAsync(filter, document, cancellationToken: cancellationToken);
    }
}

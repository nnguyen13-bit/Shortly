using MongoDB.Driver;
using Shortly.Application.Interfaces;
using Shortly.Domain.CustomDomains;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Persistence.Documents;
using Shortly.Infrastructure.Persistence.Mappers;

namespace Shortly.Infrastructure.Persistence;

public sealed class MongoCustomDomainRepository : ICustomDomainRepository
{
    private readonly IMongoCollection<CustomDomainDocument> _collection;

    public MongoCustomDomainRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<CustomDomainDocument>("domains");
    }

    public async Task<CustomDomain?> GetByPrefixAsync(DomainPrefix prefix, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CustomDomainDocument>.Filter.Eq(d => d.Prefix, prefix.Value);
        var document = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : CustomDomainMapper.ToDomain(document);
    }

    public async Task<IReadOnlyList<CustomDomain>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var filter = activeOnly.HasValue
            ? Builders<CustomDomainDocument>.Filter.Eq(d => d.IsActive, activeOnly.Value)
            : Builders<CustomDomainDocument>.Filter.Empty;

        var documents = await _collection.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(CustomDomainMapper.ToDomain).ToList();
    }

    public async Task AddAsync(CustomDomain customDomain, CancellationToken cancellationToken = default)
    {
        var document = CustomDomainMapper.ToDocument(customDomain);
        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(CustomDomain customDomain, CancellationToken cancellationToken = default)
    {
        var document = CustomDomainMapper.ToDocument(customDomain);
        var filter = Builders<CustomDomainDocument>.Filter.Eq(d => d.Id, customDomain.Id.Value);
        await _collection.ReplaceOneAsync(filter, document, cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(DomainPrefix prefix, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CustomDomainDocument>.Filter.Eq(d => d.Prefix, prefix.Value);
        return await _collection.Find(filter).AnyAsync(cancellationToken);
    }
}

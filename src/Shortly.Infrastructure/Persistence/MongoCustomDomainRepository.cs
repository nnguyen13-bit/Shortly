using MongoDB.Driver;
using Polly;
using Polly.Registry;
using Shortly.Application.Interfaces;
using Shortly.Domain.CustomDomains;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Persistence.Documents;
using Shortly.Infrastructure.Persistence.Mappers;
using Shortly.Infrastructure.Resilience;

namespace Shortly.Infrastructure.Persistence;

public sealed class MongoCustomDomainRepository : ICustomDomainRepository
{
    private readonly IMongoCollection<CustomDomainDocument> _collection;
    private readonly ResiliencePipeline _pipeline;

    public MongoCustomDomainRepository(MongoDbContext context, ResiliencePipelineProvider<string> pipelineProvider)
    {
        _collection = context.GetCollection<CustomDomainDocument>("domains");
        _pipeline = pipelineProvider.GetPipeline(ResilienceKeys.MongoDb);
    }

    public async Task<CustomDomain?> GetByPrefixAsync(DomainPrefix prefix, CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            var filter = Builders<CustomDomainDocument>.Filter.Eq(d => d.Prefix, prefix.Value);
            var document = await _collection.Find(filter).FirstOrDefaultAsync(ct);
            return document is null ? null : CustomDomainMapper.ToDomain(document);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomDomain>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            var filter = activeOnly.HasValue
                ? Builders<CustomDomainDocument>.Filter.Eq(d => d.IsActive, activeOnly.Value)
                : Builders<CustomDomainDocument>.Filter.Empty;

            var documents = await _collection.Find(filter).ToListAsync(ct);
            return (IReadOnlyList<CustomDomain>)documents.Select(CustomDomainMapper.ToDomain).ToList();
        }, cancellationToken);
    }

    public async Task AddAsync(CustomDomain customDomain, CancellationToken cancellationToken = default)
    {
        await _pipeline.ExecuteAsync(async ct =>
        {
            var document = CustomDomainMapper.ToDocument(customDomain);
            await _collection.InsertOneAsync(document, cancellationToken: ct);
        }, cancellationToken);
    }

    public async Task UpdateAsync(CustomDomain customDomain, CancellationToken cancellationToken = default)
    {
        await _pipeline.ExecuteAsync(async ct =>
        {
            var document = CustomDomainMapper.ToDocument(customDomain);
            var filter = Builders<CustomDomainDocument>.Filter.Eq(d => d.Id, customDomain.Id.Value);
            await _collection.ReplaceOneAsync(filter, document, cancellationToken: ct);
        }, cancellationToken);
    }

    public async Task<bool> ExistsAsync(DomainPrefix prefix, CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            var filter = Builders<CustomDomainDocument>.Filter.Eq(d => d.Prefix, prefix.Value);
            return await _collection.Find(filter).AnyAsync(ct);
        }, cancellationToken);
    }
}

using MongoDB.Driver;
using Polly;
using Polly.Registry;
using Shortly.Application.Interfaces;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Persistence.Documents;
using Shortly.Infrastructure.Persistence.Mappers;
using Shortly.Infrastructure.Resilience;

namespace Shortly.Infrastructure.Persistence;

public sealed class MongoLinkRepository : ILinkRepository
{
    private readonly IMongoCollection<LinkDocument> _collection;
    private readonly ResiliencePipeline _pipeline;

    public MongoLinkRepository(MongoDbContext context, ResiliencePipelineProvider<string> pipelineProvider)
    {
        _collection = context.GetCollection<LinkDocument>("links");
        _pipeline = pipelineProvider.GetPipeline(ResilienceKeys.MongoDb);
    }

    public async Task<Link?> GetByIdAsync(LinkId id, CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            var filter = Builders<LinkDocument>.Filter.Eq(d => d.Id, id.Value);
            var document = await _collection.Find(filter).FirstOrDefaultAsync(ct);
            return document is null ? null : LinkMapper.ToDomain(document);
        }, cancellationToken);
    }

    public async Task<Link?> GetByPrefixAndCodeAsync(DomainPrefix prefix, ShortCode code, CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            var filter = Builders<LinkDocument>.Filter.And(
                Builders<LinkDocument>.Filter.Eq(d => d.DomainPrefix, prefix.Value),
                Builders<LinkDocument>.Filter.Eq(d => d.ShortCode, code.Value));

            var document = await _collection.Find(filter).FirstOrDefaultAsync(ct);
            return document is null ? null : LinkMapper.ToDomain(document);
        }, cancellationToken);
    }

    public async Task AddAsync(Link link, CancellationToken cancellationToken = default)
    {
        await _pipeline.ExecuteAsync(async ct =>
        {
            var document = LinkMapper.ToDocument(link);
            await _collection.InsertOneAsync(document, cancellationToken: ct);
        }, cancellationToken);
    }

    public async Task UpdateAsync(Link link, CancellationToken cancellationToken = default)
    {
        await _pipeline.ExecuteAsync(async ct =>
        {
            var document = LinkMapper.ToDocument(link);
            var filter = Builders<LinkDocument>.Filter.Eq(d => d.Id, link.Id.Value);
            await _collection.ReplaceOneAsync(filter, document, cancellationToken: ct);
        }, cancellationToken);
    }

    public async Task<long> CountByPrefixAsync(DomainPrefix prefix, CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            var filter = Builders<LinkDocument>.Filter.And(
                Builders<LinkDocument>.Filter.Eq(d => d.DomainPrefix, prefix.Value),
                Builders<LinkDocument>.Filter.Eq(d => d.Status, LinkStatus.Active.ToString()));

            return await _collection.CountDocumentsAsync(filter, cancellationToken: ct);
        }, cancellationToken);
    }
}

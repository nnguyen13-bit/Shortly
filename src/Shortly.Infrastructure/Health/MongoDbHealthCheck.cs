using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using Shortly.Infrastructure.Persistence;

namespace Shortly.Infrastructure.Health;

public sealed class MongoDbHealthCheck : IHealthCheck
{
    private readonly MongoDbContext _context;

    public MongoDbHealthCheck(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _context.GetDatabase();
            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("MongoDB is responsive.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is not responsive.", ex);
        }
    }
}

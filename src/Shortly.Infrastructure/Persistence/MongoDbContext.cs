using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Shortly.Infrastructure.Configuration;

namespace Shortly.Infrastructure.Persistence;

public sealed class MongoDbContext
{
    private static bool _serialiserRegistered;
    private static readonly Lock _registrationLock = new();

    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        RegisterSerialiserConventions();
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);

    public IMongoDatabase GetDatabase() => _database;

    private static void RegisterSerialiserConventions()
    {
        if (_serialiserRegistered) return;

        lock (_registrationLock)
        {
            if (_serialiserRegistered) return;

            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            _serialiserRegistered = true;
        }
    }
}

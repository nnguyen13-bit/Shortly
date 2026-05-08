using Microsoft.Extensions.Options;
using Shortly.Infrastructure.Configuration;
using Shortly.Infrastructure.Persistence;
using Testcontainers.MongoDb;

namespace Shortly.Infrastructure.Tests.Persistence;

public sealed class MongoDbFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7")
        .Build();

    public MongoDbContext Context { get; private set; } = null!;
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString = _container.GetConnectionString(),
            DatabaseName = $"shortly_test_{Guid.NewGuid():N}"
        });

        Context = new MongoDbContext(settings);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("MongoDB")]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>;

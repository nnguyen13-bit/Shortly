using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shortly.Infrastructure.Health;

namespace Shortly.Infrastructure.Tests;

[Collection("MongoDB")]
public sealed class MongoDbHealthCheckTests
{
    private readonly Persistence.MongoDbFixture _fixture;

    public MongoDbHealthCheckTests(Persistence.MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CheckHealthAsync_WhenMongoDbIsRunning_ReturnsHealthy()
    {
        // Arrange
        var healthCheck = new MongoDbHealthCheck(_fixture.Context);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("mongodb", healthCheck, null, ["ready"])
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("MongoDB is responsive.", result.Description);
    }
}

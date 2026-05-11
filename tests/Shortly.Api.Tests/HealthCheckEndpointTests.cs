using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Shortly.Api.Tests;

public sealed class HealthCheckEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthCheckEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LiveEndpoint_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadyEndpoint_ReturnsStatusCode()
    {
        // Act — readiness depends on MongoDB availability; in test env it may be unhealthy
        var response = await _client.GetAsync("/health/ready");

        // Assert — either healthy (200) or unhealthy (503) is valid, but endpoint must exist
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Expected 200 or 503 but got {(int)response.StatusCode}");
    }
}

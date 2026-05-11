using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shortly.Api.Authentication;

namespace Shortly.Api.Tests;

public sealed class ApiKeyAuthenticationTests : IClassFixture<ApiKeyAuthenticationTests.AuthTestFactory>
{
    private const string ValidApiKey = "test-api-key-12345";
    private const string ClientName = "test-client";
    private readonly HttpClient _client;

    public ApiKeyAuthenticationTests(AuthTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ApiEndpoint_WithoutApiKey_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/Domains");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoint_WithInvalidApiKey_Returns401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Domains");
        request.Headers.Add("X-Api-Key", "invalid-key");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoint_WithEmptyApiKey_Returns401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Domains");
        request.Headers.Add("X-Api-Key", "");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoint_WithValidApiKey_DoesNotReturn401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Domains");
        request.Headers.Add("X-Api-Key", ValidApiKey);

        // Act
        var response = await _client.SendAsync(request);

        // Assert — may be 200 or 5xx (no MongoDB), but NOT 401
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RedirectEndpoint_WithoutApiKey_DoesNotReturn401()
    {
        // Act — redirect to a non-existent link; expect 404, not 401
        var response = await _client.GetAsync("/ab/abcde");

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthLiveEndpoint_WithoutApiKey_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReadyEndpoint_WithoutApiKey_DoesNotReturn401()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert — 200 or 503, but not 401
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unauthorized_Response_ContainsErrorMessage()
    {
        // Act
        var response = await _client.GetAsync("/api/Domains");
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains("API key", body.Error);
        Assert.Equal(401, body.StatusCode);
    }

    private sealed record ErrorBody(string Error, int StatusCode);

    public sealed class AuthTestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<ApiKeySettings>(opts =>
                {
                    opts.Clients =
                    [
                        new ApiKeyClient { Name = ClientName, Key = ValidApiKey }
                    ];
                });
            });
        }
    }
}

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Api.Authentication;
using Shortly.Api.RateLimiting;

namespace Shortly.Api.Tests;

public sealed class RateLimitingTests : IClassFixture<RateLimitingTests.RateLimitTestFactory>
{
    private const string ValidApiKey = "test-ratelimit-key";
    private readonly HttpClient _client;

    public RateLimitingTests(RateLimitTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    private HttpRequestMessage AuthorisedGet(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Api-Key", ValidApiKey);
        return request;
    }

    [Fact]
    public async Task AuthenticatedEndpoint_WithinLimit_Returns200()
    {
        var request = AuthorisedGet("/api/Domains");
        var response = await _client.SendAsync(request);

        // Should succeed (200) — we're within the 3-request limit
        Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedEndpoint_ExceedsLimit_Returns429()
    {
        // Exhaust the per-client limit (set to 3 in test factory)
        for (var i = 0; i < 3; i++)
        {
            var r = AuthorisedGet("/api/Domains");
            await _client.SendAsync(r);
        }

        // The 4th request should be rate-limited
        var request = AuthorisedGet("/api/Domains");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task RedirectEndpoint_ExceedsIpLimit_Returns429()
    {
        // Use valid short-code format: 2-4 lowercase alpha prefix + 5-8 alphanumeric code
        for (var i = 0; i < 3; i++)
        {
            await _client.GetAsync($"/ab/ABCDE");
        }

        // The 4th request should be rate-limited
        var response = await _client.GetAsync("/ab/ABCDE");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoints_NotRateLimited()
    {
        // Health endpoints have no rate limiting policy — should always work
        for (var i = 0; i < 10; i++)
        {
            var response = await _client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task RateLimited_Response_Contains429Body()
    {
        // Exhaust the per-client limit
        for (var i = 0; i < 3; i++)
        {
            var r = AuthorisedGet("/api/Domains");
            await _client.SendAsync(r);
        }

        var request = AuthorisedGet("/api/Domains");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Too many requests", body);
    }

    public sealed class RateLimitTestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<ApiKeySettings>(opts =>
                {
                    opts.Clients =
                    [
                        new ApiKeyClient { Name = "ratelimit-client", Key = ValidApiKey }
                    ];
                });
            });

            // Override rate limiting config to very low limits for testing
            builder.UseSetting("RateLimiting:ClientPermitLimit", "3");
            builder.UseSetting("RateLimiting:ClientWindowSeconds", "60");
            builder.UseSetting("RateLimiting:IpPermitLimit", "3");
            builder.UseSetting("RateLimiting:IpWindowSeconds", "60");
        }
    }
}

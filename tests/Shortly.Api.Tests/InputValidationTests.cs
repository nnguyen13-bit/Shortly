using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shortly.Api.Authentication;

namespace Shortly.Api.Tests;

public sealed class InputValidationTests : IClassFixture<InputValidationTests.ValidationTestFactory>
{
    private const string ValidApiKey = "test-validation-key";
    private readonly HttpClient _client;

    public InputValidationTests(ValidationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    private HttpRequestMessage AuthorisedPost(string url, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Api-Key", ValidApiKey);
        return request;
    }

    [Fact]
    public async Task CreateLink_MissingDomainPrefix_Returns400()
    {
        var request = AuthorisedPost("/api/Links", """
            {"destinationUrl": "https://example.com", "createdBy": "test"}
        """);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLink_InvalidPrefixFormat_Returns400()
    {
        var request = AuthorisedPost("/api/Links", """
            {"domainPrefix": "INVALID!", "destinationUrl": "https://example.com", "createdBy": "test"}
        """);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLink_PrefixTooLong_Returns400()
    {
        var request = AuthorisedPost("/api/Links", """
            {"domainPrefix": "abcde", "destinationUrl": "https://example.com", "createdBy": "test"}
        """);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLink_InvalidUrl_Returns400()
    {
        var request = AuthorisedPost("/api/Links", """
            {"domainPrefix": "ab", "destinationUrl": "not-a-url", "createdBy": "test"}
        """);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLink_UrlTooLong_Returns400()
    {
        var longUrl = "https://example.com/" + new string('a', 2040);
        var json = $$"""{"domainPrefix": "ab", "destinationUrl": "{{longUrl}}", "createdBy": "test"}""";
        var request = AuthorisedPost("/api/Links", json);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLink_MissingCreatedBy_Returns400()
    {
        var request = AuthorisedPost("/api/Links", """
            {"domainPrefix": "ab", "destinationUrl": "https://example.com"}
        """);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLink_TooManyTags_Returns400()
    {
        var tags = string.Join(",", Enumerable.Range(1, 11).Select(i => $"\"k{i}\": \"v{i}\""));
        var json = $"{{\"domainPrefix\": \"ab\", \"destinationUrl\": \"https://example.com\", \"createdBy\": \"test\", \"tags\": {{ {tags} }}}}";
        var request = AuthorisedPost("/api/Links", json);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDomain_MissingName_Returns400()
    {
        var request = AuthorisedPost("/api/Domains", """
            {"prefix": "ab"}
        """);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDomain_PrefixTooShort_Returns400()
    {
        var request = AuthorisedPost("/api/Domains", """
            {"prefix": "a", "name": "Test"}
        """);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDomain_NameTooLong_Returns400()
    {
        var longName = new string('a', 201);
        var json = $$"""{"prefix": "ab", "name": "{{longName}}"}""";
        var request = AuthorisedPost("/api/Domains", json);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDomain_DescriptionTooLong_Returns400()
    {
        var longDesc = new string('a', 501);
        var json = $"{{\"prefix\": \"ab\", \"name\": \"Test\", \"description\": \"{longDesc}\"}}";
        var request = AuthorisedPost("/api/Domains", json);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLink_EmptyBody_Returns400()
    {
        var request = AuthorisedPost("/api/Links", "{}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public sealed class ValidationTestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<ApiKeySettings>(opts =>
                {
                    opts.Clients =
                    [
                        new ApiKeyClient { Name = "test-client", Key = ValidApiKey }
                    ];
                });
            });
        }
    }
}

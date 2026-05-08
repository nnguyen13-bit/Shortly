using Shortly.Domain.LinkManagement;

namespace Shortly.Domain.Tests.LinkManagement;

public sealed class DestinationUrlTests
{
    // --- Valid URLs ---

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/path")]
    [InlineData("https://example.com/path?q=1&r=2")]
    [InlineData("https://example.com/path#fragment")]
    [InlineData("http://example.com")]
    [InlineData("https://sub.domain.example.com/deep/path")]
    public void Constructor_WithValidUrl_SetsValue(string url)
    {
        var destinationUrl = new DestinationUrl(url);

        Assert.Equal(url, destinationUrl.Value);
    }

    // --- Empty / Null ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyUrl_ThrowsArgumentException(string? url)
    {
        Assert.Throws<ArgumentException>(() => new DestinationUrl(url!));
    }

    // --- Invalid Schemes ---

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("file:///local/path")]
    [InlineData("mailto:user@example.com")]
    public void Constructor_WithNonHttpScheme_ThrowsArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() => new DestinationUrl(url));
    }

    // --- Forbidden Schemes ---

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<h1>test</h1>")]
    public void Constructor_WithForbiddenScheme_ThrowsArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() => new DestinationUrl(url));
    }

    // --- Not Absolute URI ---

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("/relative/path")]
    [InlineData("example.com")]
    public void Constructor_WithRelativeUrl_ThrowsArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() => new DestinationUrl(url));
    }

    // --- Max Length ---

    [Fact]
    public void Constructor_WithUrlExceeding2048Chars_ThrowsArgumentException()
    {
        var longUrl = "https://example.com/" + new string('a', 2030);

        Assert.Throws<ArgumentException>(() => new DestinationUrl(longUrl));
    }

    [Fact]
    public void Constructor_WithUrlAt2048Chars_Succeeds()
    {
        var url = "https://example.com/" + new string('a', 2028);
        Assert.Equal(2048, url.Length);

        var destinationUrl = new DestinationUrl(url);

        Assert.Equal(url, destinationUrl.Value);
    }

    // --- Equality ---

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var a = new DestinationUrl("https://example.com");
        var b = new DestinationUrl("https://example.com");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var a = new DestinationUrl("https://example.com");
        var b = new DestinationUrl("https://other.com");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var url = new DestinationUrl("https://example.com");

        Assert.Equal("https://example.com", url.ToString());
    }
}

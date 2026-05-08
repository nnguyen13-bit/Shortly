using Shortly.Domain.LinkManagement;

namespace Shortly.Domain.Tests.LinkManagement;

public sealed class DomainPrefixTests
{
    // --- Valid Prefixes ---

    [Theory]
    [InlineData("ho")]       // 2 chars (min)
    [InlineData("abc")]      // 3 chars
    [InlineData("abcd")]     // 4 chars (max)
    [InlineData("12")]       // All digits
    [InlineData("a1b2")]     // Mixed
    public void Constructor_WithValidPrefix_SetsValue(string prefix)
    {
        var domainPrefix = new DomainPrefix(prefix);

        Assert.Equal(prefix, domainPrefix.Value);
    }

    // --- Invalid Prefixes ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyPrefix_ThrowsArgumentException(string? prefix)
    {
        Assert.Throws<ArgumentException>(() => new DomainPrefix(prefix!));
    }

    [Fact]
    public void Constructor_WithTooShortPrefix_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new DomainPrefix("a")); // 1 char
    }

    [Fact]
    public void Constructor_WithTooLongPrefix_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new DomainPrefix("abcde")); // 5 chars
    }

    [Theory]
    [InlineData("AB")]       // Uppercase
    [InlineData("Ab")]       // Mixed case
    [InlineData("a-b")]      // Hyphen
    [InlineData("a_b")]      // Underscore
    [InlineData("a b")]      // Space
    [InlineData("a!b")]      // Special char
    public void Constructor_WithInvalidCharacters_ThrowsArgumentException(string prefix)
    {
        Assert.Throws<ArgumentException>(() => new DomainPrefix(prefix));
    }

    // --- Equality ---

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var a = new DomainPrefix("ho");
        var b = new DomainPrefix("ho");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var a = new DomainPrefix("ho");
        var b = new DomainPrefix("ab");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var prefix = new DomainPrefix("ho");

        Assert.Equal("ho", prefix.ToString());
    }
}

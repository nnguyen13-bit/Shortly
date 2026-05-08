using Shortly.Domain.LinkManagement;

namespace Shortly.Domain.Tests.LinkManagement;

public sealed class ShortCodeTests
{
    // --- Valid Codes ---

    [Theory]
    [InlineData("aBcD1")]      // 5 chars (min)
    [InlineData("aBcD12")]     // 6 chars
    [InlineData("aBcD123")]    // 7 chars
    [InlineData("aBcD1234")]   // 8 chars (max)
    [InlineData("AAAAA")]      // All uppercase
    [InlineData("aaaaa")]      // All lowercase
    [InlineData("12345")]      // All digits
    [InlineData("aA1bB2cC")]   // Mixed 8 chars
    public void Constructor_WithValidCode_SetsValue(string code)
    {
        var shortCode = new ShortCode(code);

        Assert.Equal(code, shortCode.Value);
    }

    // --- Invalid Codes ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyCode_ThrowsArgumentException(string? code)
    {
        Assert.Throws<ArgumentException>(() => new ShortCode(code!));
    }

    [Theory]
    [InlineData("abcd")]       // 4 chars (too short)
    [InlineData("a")]          // 1 char
    [InlineData("ab")]         // 2 chars
    public void Constructor_WithTooShortCode_ThrowsArgumentException(string code)
    {
        Assert.Throws<ArgumentException>(() => new ShortCode(code));
    }

    [Fact]
    public void Constructor_WithTooLongCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ShortCode("abcde1234")); // 9 chars
    }

    [Theory]
    [InlineData("abc-d1")]     // Hyphen
    [InlineData("abc_d1")]     // Underscore
    [InlineData("abc d1")]     // Space
    [InlineData("abc!d1")]     // Special char
    public void Constructor_WithInvalidCharacters_ThrowsArgumentException(string code)
    {
        Assert.Throws<ArgumentException>(() => new ShortCode(code));
    }

    // --- Equality ---

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var a = new ShortCode("aBcD1");
        var b = new ShortCode("aBcD1");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var a = new ShortCode("aBcD1");
        var b = new ShortCode("xYzW2");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var code = new ShortCode("aBcD1");

        Assert.Equal("aBcD1", code.ToString());
    }
}

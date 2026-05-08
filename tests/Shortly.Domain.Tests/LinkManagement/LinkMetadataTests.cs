using Shortly.Domain.LinkManagement;

namespace Shortly.Domain.Tests.LinkManagement;

public sealed class LinkMetadataTests
{
    // --- Valid Metadata ---

    [Fact]
    public void Constructor_WithNullTags_CreatesEmptyMetadata()
    {
        var metadata = new LinkMetadata();

        Assert.Empty(metadata.Tags);
    }

    [Fact]
    public void Constructor_WithEmptyDictionary_CreatesEmptyMetadata()
    {
        var metadata = new LinkMetadata(new Dictionary<string, string>());

        Assert.Empty(metadata.Tags);
    }

    [Fact]
    public void Constructor_WithValidTags_SetsTags()
    {
        var tags = new Dictionary<string, string>
        {
            ["team"] = "platform",
            ["env"] = "production"
        };

        var metadata = new LinkMetadata(tags);

        Assert.Equal(2, metadata.Tags.Count);
        Assert.Equal("platform", metadata.Tags["team"]);
        Assert.Equal("production", metadata.Tags["env"]);
    }

    [Fact]
    public void Constructor_With10Tags_Succeeds()
    {
        var tags = Enumerable.Range(1, 10)
            .ToDictionary(i => $"key{i}", i => $"value{i}");

        var metadata = new LinkMetadata(tags);

        Assert.Equal(10, metadata.Tags.Count);
    }

    // --- Validation ---

    [Fact]
    public void Constructor_With11Tags_ThrowsArgumentException()
    {
        var tags = Enumerable.Range(1, 11)
            .ToDictionary(i => $"key{i}", i => $"value{i}");

        var ex = Assert.Throws<ArgumentException>(() => new LinkMetadata(tags));
        Assert.Contains("10", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyKey_ThrowsArgumentException()
    {
        var tags = new Dictionary<string, string> { [""] = "value" };

        Assert.Throws<ArgumentException>(() => new LinkMetadata(tags));
    }

    [Fact]
    public void Constructor_WithWhitespaceKey_ThrowsArgumentException()
    {
        var tags = new Dictionary<string, string> { ["   "] = "value" };

        Assert.Throws<ArgumentException>(() => new LinkMetadata(tags));
    }

    [Fact]
    public void Constructor_WithKeyExceeding50Chars_ThrowsArgumentException()
    {
        var longKey = new string('k', 51);
        var tags = new Dictionary<string, string> { [longKey] = "value" };

        var ex = Assert.Throws<ArgumentException>(() => new LinkMetadata(tags));
        Assert.Contains("50", ex.Message);
    }

    [Fact]
    public void Constructor_WithKeyAt50Chars_Succeeds()
    {
        var key = new string('k', 50);
        var tags = new Dictionary<string, string> { [key] = "value" };

        var metadata = new LinkMetadata(tags);

        Assert.Single(metadata.Tags);
    }

    [Fact]
    public void Constructor_WithValueExceeding200Chars_ThrowsArgumentException()
    {
        var longValue = new string('v', 201);
        var tags = new Dictionary<string, string> { ["key"] = longValue };

        var ex = Assert.Throws<ArgumentException>(() => new LinkMetadata(tags));
        Assert.Contains("200", ex.Message);
    }

    [Fact]
    public void Constructor_WithValueAt200Chars_Succeeds()
    {
        var value = new string('v', 200);
        var tags = new Dictionary<string, string> { ["key"] = value };

        var metadata = new LinkMetadata(tags);

        Assert.Single(metadata.Tags);
    }

    // --- Immutability ---

    [Fact]
    public void Tags_AreImmutable_OriginalDictionaryChangesDoNotAffect()
    {
        var tags = new Dictionary<string, string> { ["key"] = "value" };
        var metadata = new LinkMetadata(tags);

        tags["key"] = "modified";

        Assert.Equal("value", metadata.Tags["key"]);
    }

    // --- Equality ---

    [Fact]
    public void Equals_SameTags_ReturnsTrue()
    {
        var a = new LinkMetadata(new Dictionary<string, string> { ["k1"] = "v1", ["k2"] = "v2" });
        var b = new LinkMetadata(new Dictionary<string, string> { ["k1"] = "v1", ["k2"] = "v2" });

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentTags_ReturnsFalse()
    {
        var a = new LinkMetadata(new Dictionary<string, string> { ["k1"] = "v1" });
        var b = new LinkMetadata(new Dictionary<string, string> { ["k1"] = "v2" });

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_BothEmpty_ReturnsTrue()
    {
        var a = new LinkMetadata();
        var b = new LinkMetadata();

        Assert.Equal(a, b);
    }
}

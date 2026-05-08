using Shortly.Domain.Common;
using Shortly.Domain.LinkManagement;

namespace Shortly.Domain.Tests.LinkManagement;

public sealed class LinkTests
{
    private static readonly ShortCode ValidCode = new("aBc12");
    private static readonly DomainPrefix ValidPrefix = new("ho");
    private static readonly DestinationUrl ValidUrl = new("https://example.com/page");
    private const string ValidCreatedBy = "test-user";

    // --- Create: Happy Path ---

    [Fact]
    public void Create_WithValidInputs_ReturnsActiveLink()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);

        Assert.Equal(LinkStatus.Active, link.Status);
        Assert.Equal(ValidCode, link.ShortCode);
        Assert.Equal(ValidPrefix, link.DomainPrefix);
        Assert.Equal(ValidUrl, link.DestinationUrl);
        Assert.Equal(ValidCreatedBy, link.CreatedBy);
        Assert.Null(link.ExpiresAt);
        Assert.Empty(link.Metadata.Tags);
    }

    [Fact]
    public void Create_WithValidInputs_SetsIdAndCreatedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);
        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(default, link.Id);
        Assert.InRange(link.CreatedAt, before, after);
    }

    [Fact]
    public void Create_WithValidInputs_IncrementsVersion()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);

        Assert.Equal(1, link.Version);
    }

    [Fact]
    public void Create_WithValidInputs_RaisesLinkCreatedEvent()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);

        var domainEvent = Assert.Single(link.DomainEvents);
        var created = Assert.IsType<LinkCreatedEvent>(domainEvent);
        Assert.Equal(link.Id, created.LinkId);
        Assert.Equal(ValidPrefix, created.DomainPrefix);
        Assert.Equal(ValidCode, created.ShortCode);
        Assert.Equal(ValidUrl, created.DestinationUrl);
    }

    [Fact]
    public void Create_WithExpiresAt_SetsExpiry()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy, expiresAt);

        Assert.Equal(expiresAt, link.ExpiresAt);
    }

    [Fact]
    public void Create_WithMetadata_SetsMetadata()
    {
        var metadata = new LinkMetadata(new Dictionary<string, string> { ["team"] = "platform" });

        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy, metadata: metadata);

        Assert.Equal("platform", link.Metadata.Tags["team"]);
    }

    // --- Create: Validation ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCreatedBy_ThrowsArgumentException(string? createdBy)
    {
        Assert.Throws<ArgumentException>(() =>
            Link.Create(ValidCode, ValidPrefix, ValidUrl, createdBy!));
    }

    [Fact]
    public void Create_WithPastExpiry_ThrowsLinkDomainException()
    {
        var pastDate = DateTimeOffset.UtcNow.AddMinutes(-1);

        var ex = Assert.Throws<LinkDomainException>(() =>
            Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy, pastDate));

        Assert.Contains("past", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithNullShortCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Link.Create(null!, ValidPrefix, ValidUrl, ValidCreatedBy));
    }

    [Fact]
    public void Create_WithNullDomainPrefix_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Link.Create(ValidCode, null!, ValidUrl, ValidCreatedBy));
    }

    [Fact]
    public void Create_WithNullDestinationUrl_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Link.Create(ValidCode, ValidPrefix, null!, ValidCreatedBy));
    }

    // --- Disable ---

    [Fact]
    public void Disable_ActiveLink_SetsStatusToDisabled()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);
        link.ClearDomainEvents();

        link.Disable();

        Assert.Equal(LinkStatus.Disabled, link.Status);
    }

    [Fact]
    public void Disable_ActiveLink_IncrementsVersion()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);
        var versionBefore = link.Version;

        link.Disable();

        Assert.Equal(versionBefore + 1, link.Version);
    }

    [Fact]
    public void Disable_ActiveLink_RaisesLinkDisabledEvent()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);
        link.ClearDomainEvents();

        link.Disable();

        var domainEvent = Assert.Single(link.DomainEvents);
        var disabled = Assert.IsType<LinkDisabledEvent>(domainEvent);
        Assert.Equal(link.Id, disabled.LinkId);
        Assert.Equal(ValidPrefix, disabled.DomainPrefix);
        Assert.Equal(ValidCode, disabled.ShortCode);
    }

    [Fact]
    public void Disable_AlreadyDisabledLink_IsIdempotent()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);
        link.Disable();
        var versionAfterFirst = link.Version;
        link.ClearDomainEvents();

        link.Disable(); // Second call

        Assert.Equal(LinkStatus.Disabled, link.Status);
        Assert.Equal(versionAfterFirst, link.Version); // No version bump
        Assert.Empty(link.DomainEvents); // No event raised
    }

    // --- IsExpired ---

    [Fact]
    public void IsExpired_NoExpiryDate_ReturnsFalse()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);

        Assert.False(link.IsExpired());
    }

    [Fact]
    public void IsExpired_FutureExpiryDate_ReturnsFalse()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy,
            expiresAt: DateTimeOffset.UtcNow.AddHours(1));

        Assert.False(link.IsExpired());
    }

    // --- IsRedirectable ---

    [Fact]
    public void IsRedirectable_ActiveLinkNoExpiry_ReturnsTrue()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);

        Assert.True(link.IsRedirectable());
    }

    [Fact]
    public void IsRedirectable_DisabledLink_ReturnsFalse()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);
        link.Disable();

        Assert.False(link.IsRedirectable());
    }

    [Fact]
    public void IsRedirectable_ActiveLinkWithFutureExpiry_ReturnsTrue()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy,
            expiresAt: DateTimeOffset.UtcNow.AddHours(1));

        Assert.True(link.IsRedirectable());
    }

    // --- ClearDomainEvents ---

    [Fact]
    public void ClearDomainEvents_AfterCreate_ClearsEvents()
    {
        var link = Link.Create(ValidCode, ValidPrefix, ValidUrl, ValidCreatedBy);
        Assert.NotEmpty(link.DomainEvents);

        link.ClearDomainEvents();

        Assert.Empty(link.DomainEvents);
    }
}

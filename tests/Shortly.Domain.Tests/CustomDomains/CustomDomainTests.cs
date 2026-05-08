using Shortly.Domain.CustomDomains;
using Shortly.Domain.LinkManagement;

namespace Shortly.Domain.Tests.CustomDomains;

public sealed class CustomDomainTests
{
    private static readonly DomainPrefix ValidPrefix = new("ho");
    private const string ValidName = "Handover";
    private const string ValidDescription = "Handover team short links";

    // --- Register: Happy Path ---

    [Fact]
    public void Register_WithValidInputs_ReturnsActiveDomain()
    {
        var domain = CustomDomain.Register(ValidPrefix, ValidName, ValidDescription);

        Assert.Equal(ValidPrefix, domain.Prefix);
        Assert.Equal(ValidName, domain.Name);
        Assert.Equal(ValidDescription, domain.Description);
        Assert.True(domain.IsActive);
    }

    [Fact]
    public void Register_WithValidInputs_SetsIdAndCreatedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var domain = CustomDomain.Register(ValidPrefix, ValidName);
        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(default, domain.Id);
        Assert.InRange(domain.CreatedAt, before, after);
    }

    [Fact]
    public void Register_WithValidInputs_IncrementsVersion()
    {
        var domain = CustomDomain.Register(ValidPrefix, ValidName);

        Assert.Equal(1, domain.Version);
    }

    [Fact]
    public void Register_WithValidInputs_RaisesCustomDomainRegisteredEvent()
    {
        var domain = CustomDomain.Register(ValidPrefix, ValidName);

        var domainEvent = Assert.Single(domain.DomainEvents);
        var registered = Assert.IsType<CustomDomainRegisteredEvent>(domainEvent);
        Assert.Equal(domain.Id, registered.CustomDomainId);
        Assert.Equal(ValidPrefix, registered.Prefix);
        Assert.Equal(ValidName, registered.Name);
    }

    [Fact]
    public void Register_WithoutDescription_SetsDescriptionToNull()
    {
        var domain = CustomDomain.Register(ValidPrefix, ValidName);

        Assert.Null(domain.Description);
    }

    // --- Register: Validation ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithEmptyName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            CustomDomain.Register(ValidPrefix, name!));
    }

    [Fact]
    public void Register_WithNullPrefix_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CustomDomain.Register(null!, ValidName));
    }

    // --- Deactivate ---

    [Fact]
    public void Deactivate_ActiveDomain_SetsIsActiveToFalse()
    {
        var domain = CustomDomain.Register(ValidPrefix, ValidName);
        domain.ClearDomainEvents();

        domain.Deactivate();

        Assert.False(domain.IsActive);
    }

    [Fact]
    public void Deactivate_ActiveDomain_IncrementsVersion()
    {
        var domain = CustomDomain.Register(ValidPrefix, ValidName);
        var versionBefore = domain.Version;

        domain.Deactivate();

        Assert.Equal(versionBefore + 1, domain.Version);
    }

    [Fact]
    public void Deactivate_ActiveDomain_RaisesCustomDomainDeactivatedEvent()
    {
        var domain = CustomDomain.Register(ValidPrefix, ValidName);
        domain.ClearDomainEvents();

        domain.Deactivate();

        var domainEvent = Assert.Single(domain.DomainEvents);
        var deactivated = Assert.IsType<CustomDomainDeactivatedEvent>(domainEvent);
        Assert.Equal(domain.Id, deactivated.CustomDomainId);
        Assert.Equal(ValidPrefix, deactivated.Prefix);
    }

    [Fact]
    public void Deactivate_AlreadyInactiveDomain_IsIdempotent()
    {
        var domain = CustomDomain.Register(ValidPrefix, ValidName);
        domain.Deactivate();
        var versionAfterFirst = domain.Version;
        domain.ClearDomainEvents();

        domain.Deactivate(); // Second call

        Assert.False(domain.IsActive);
        Assert.Equal(versionAfterFirst, domain.Version);
        Assert.Empty(domain.DomainEvents);
    }
}

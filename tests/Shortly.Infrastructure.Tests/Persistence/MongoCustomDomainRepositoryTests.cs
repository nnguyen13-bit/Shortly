using Shortly.Domain.CustomDomains;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Persistence;

namespace Shortly.Infrastructure.Tests.Persistence;

[Collection("MongoDB")]
public sealed class MongoCustomDomainRepositoryTests
{
    private readonly MongoCustomDomainRepository _repository;

    public MongoCustomDomainRepositoryTests(MongoDbFixture fixture)
    {
        _repository = new MongoCustomDomainRepository(fixture.Context);
    }

    // --- AddAsync + GetByPrefixAsync ---

    [Fact]
    public async Task AddAsync_ThenGetByPrefix_ReturnsDomain()
    {
        var prefix = new DomainPrefix(GenerateUniquePrefix());
        var domain = CustomDomain.Register(prefix, "Test Domain", "A test domain");

        await _repository.AddAsync(domain);
        var retrieved = await _repository.GetByPrefixAsync(prefix);

        Assert.NotNull(retrieved);
        Assert.Equal(domain.Id, retrieved.Id);
        Assert.Equal(prefix, retrieved.Prefix);
        Assert.Equal("Test Domain", retrieved.Name);
        Assert.Equal("A test domain", retrieved.Description);
        Assert.True(retrieved.IsActive);
    }

    [Fact]
    public async Task GetByPrefixAsync_NonExistentPrefix_ReturnsNull()
    {
        var result = await _repository.GetByPrefixAsync(new DomainPrefix("zz"));

        Assert.Null(result);
    }

    // --- ExistsAsync ---

    [Fact]
    public async Task ExistsAsync_ExistingPrefix_ReturnsTrue()
    {
        var prefix = new DomainPrefix(GenerateUniquePrefix());
        var domain = CustomDomain.Register(prefix, "Exists Domain");
        await _repository.AddAsync(domain);

        var exists = await _repository.ExistsAsync(prefix);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_NonExistentPrefix_ReturnsFalse()
    {
        var exists = await _repository.ExistsAsync(new DomainPrefix("zz"));

        Assert.False(exists);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_DeactivatedDomain_PersistsChange()
    {
        var prefix = new DomainPrefix(GenerateUniquePrefix());
        var domain = CustomDomain.Register(prefix, "Deactivate Me");
        await _repository.AddAsync(domain);

        domain.Deactivate();
        await _repository.UpdateAsync(domain);

        var retrieved = await _repository.GetByPrefixAsync(prefix);

        Assert.NotNull(retrieved);
        Assert.False(retrieved.IsActive);
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_NoFilter_ReturnsAllDomains()
    {
        var prefix1 = new DomainPrefix(GenerateUniquePrefix());
        var prefix2 = new DomainPrefix(GenerateUniquePrefix());

        await _repository.AddAsync(CustomDomain.Register(prefix1, "Domain 1"));
        await _repository.AddAsync(CustomDomain.Register(prefix2, "Domain 2"));

        var all = await _repository.GetAllAsync();

        Assert.True(all.Count >= 2);
    }

    [Fact]
    public async Task GetAllAsync_ActiveOnly_ExcludesInactive()
    {
        var activePrefix = new DomainPrefix(GenerateUniquePrefix());
        var inactivePrefix = new DomainPrefix(GenerateUniquePrefix());

        var active = CustomDomain.Register(activePrefix, "Active");
        var inactive = CustomDomain.Register(inactivePrefix, "Inactive");
        inactive.Deactivate();

        await _repository.AddAsync(active);
        await _repository.AddAsync(inactive);

        var activeOnly = await _repository.GetAllAsync(activeOnly: true);

        Assert.DoesNotContain(activeOnly, d => d.Prefix.Value == inactivePrefix.Value);
        Assert.Contains(activeOnly, d => d.Prefix.Value == activePrefix.Value);
    }

    [Fact]
    public async Task GetAllAsync_InactiveOnly_ExcludesActive()
    {
        var activePrefix = new DomainPrefix(GenerateUniquePrefix());
        var inactivePrefix = new DomainPrefix(GenerateUniquePrefix());

        var active = CustomDomain.Register(activePrefix, "Active");
        var inactive = CustomDomain.Register(inactivePrefix, "Inactive");
        inactive.Deactivate();

        await _repository.AddAsync(active);
        await _repository.AddAsync(inactive);

        var inactiveOnly = await _repository.GetAllAsync(activeOnly: false);

        Assert.Contains(inactiveOnly, d => d.Prefix.Value == inactivePrefix.Value);
        Assert.DoesNotContain(inactiveOnly, d => d.Prefix.Value == activePrefix.Value);
    }

    // --- Null description ---

    [Fact]
    public async Task AddAsync_WithNullDescription_PersistsCorrectly()
    {
        var prefix = new DomainPrefix(GenerateUniquePrefix());
        var domain = CustomDomain.Register(prefix, "No Description");

        await _repository.AddAsync(domain);
        var retrieved = await _repository.GetByPrefixAsync(prefix);

        Assert.NotNull(retrieved);
        Assert.Null(retrieved.Description);
    }

    // --- Helper ---

    private static string GenerateUniquePrefix()
    {
        return "t" + Guid.NewGuid().ToString("N")[..2];
    }
}

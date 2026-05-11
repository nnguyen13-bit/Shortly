using Shortly.Application.Interfaces;
using Shortly.Application.Links;
using Shortly.Domain.Common;
using Shortly.Domain.LinkManagement;

namespace Shortly.Application.Tests;

public sealed class RedirectServiceCacheTests
{
    private readonly InMemoryLinkRepository _linkRepo = new();
    private readonly StubEventPublisher _eventPublisher = new();
    private readonly InMemoryRedirectCache _cache = new();
    private readonly RedirectService _service;

    public RedirectServiceCacheTests()
    {
        _service = new RedirectService(_linkRepo, _eventPublisher, _cache);
    }

    [Fact]
    public async Task Resolve_CacheMiss_QueriesDatabase()
    {
        var link = CreateActiveLink("ab", "ABCDE", "https://example.com");
        _linkRepo.Add(link);

        var result = await _service.ResolveAsync("ab", "ABCDE");

        Assert.True(result.IsFound);
        Assert.Equal("https://example.com", result.DestinationUrl);
    }

    [Fact]
    public async Task Resolve_CacheMiss_PopulatesCache()
    {
        var link = CreateActiveLink("ab", "FGHIJ", "https://cached.com");
        _linkRepo.Add(link);

        await _service.ResolveAsync("ab", "FGHIJ");

        Assert.Equal("https://cached.com", _cache.Get("ab", "FGHIJ"));
    }

    [Fact]
    public async Task Resolve_CacheHit_SkipsDatabase()
    {
        _cache.Set("ab", "KLMNO", "https://from-cache.com");
        // No link in database — if it queries DB, it would return NotFound

        var result = await _service.ResolveAsync("ab", "KLMNO");

        Assert.True(result.IsFound);
        Assert.Equal("https://from-cache.com", result.DestinationUrl);
    }

    [Fact]
    public async Task Resolve_CacheHit_StillPublishesEvent()
    {
        _cache.Set("cd", "PQRST", "https://event-test.com");

        await _service.ResolveAsync("cd", "PQRST");

        Assert.Single(_eventPublisher.PublishedEvents);
    }

    [Fact]
    public async Task Resolve_NotInCacheOrDatabase_ReturnsNotFound()
    {
        var result = await _service.ResolveAsync("zz", "NOPE1");

        Assert.False(result.IsFound);
        Assert.False(result.IsGone);
    }

    [Fact]
    public async Task Resolve_DisabledLink_NotCached_ReturnsGone()
    {
        var link = CreateActiveLink("ab", "DISAB", "https://disabled.com");
        link.Disable();
        _linkRepo.Add(link);

        var result = await _service.ResolveAsync("ab", "DISAB");

        Assert.True(result.IsGone);
        // Should NOT cache disabled links
        Assert.Null(_cache.Get("ab", "DISAB"));
    }

    private static Link CreateActiveLink(string prefix, string code, string url)
    {
        return Link.Create(
            new ShortCode(code),
            new DomainPrefix(prefix),
            new DestinationUrl(url),
            "test-user");
    }

    private sealed class InMemoryLinkRepository : ILinkRepository
    {
        private readonly List<Link> _links = [];

        public void Add(Link link) => _links.Add(link);

        public Task<Link?> GetByPrefixAndCodeAsync(DomainPrefix prefix, ShortCode code, CancellationToken cancellationToken = default)
        {
            var link = _links.Find(l =>
                l.DomainPrefix.Value == prefix.Value && l.ShortCode.Value == code.Value);
            return Task.FromResult(link);
        }

        public Task<Link?> GetByIdAsync(LinkId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_links.Find(l => l.Id == id));

        public Task AddAsync(Link link, CancellationToken cancellationToken = default)
        {
            _links.Add(link);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Link link, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<long> CountByPrefixAsync(DomainPrefix prefix, CancellationToken cancellationToken = default)
            => Task.FromResult((long)_links.Count(l => l.DomainPrefix.Value == prefix.Value));
    }

    private sealed class StubEventPublisher : IEventPublisher
    {
        public List<DomainEvent> PublishedEvents { get; } = [];

        public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            PublishedEvents.Add(domainEvent);
            return Task.CompletedTask;
        }

        public Task PublishFireAndForgetAsync(DomainEvent domainEvent)
        {
            PublishedEvents.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRedirectCache : IRedirectCache
    {
        private readonly Dictionary<string, string> _store = new();

        public Task<string?> GetDestinationUrlAsync(string domainPrefix, string shortCode, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(Key(domainPrefix, shortCode), out var value);
            return Task.FromResult(value);
        }

        public Task SetDestinationUrlAsync(string domainPrefix, string shortCode, string destinationUrl, CancellationToken cancellationToken = default)
        {
            _store[Key(domainPrefix, shortCode)] = destinationUrl;
            return Task.CompletedTask;
        }

        public Task EvictAsync(string domainPrefix, string shortCode, CancellationToken cancellationToken = default)
        {
            _store.Remove(Key(domainPrefix, shortCode));
            return Task.CompletedTask;
        }

        public void Set(string domainPrefix, string shortCode, string destinationUrl)
            => _store[Key(domainPrefix, shortCode)] = destinationUrl;

        public string? Get(string domainPrefix, string shortCode)
            => _store.GetValueOrDefault(Key(domainPrefix, shortCode));

        private static string Key(string prefix, string code) => $"{prefix}:{code}";
    }
}

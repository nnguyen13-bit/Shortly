using Shortly.Application.Interfaces;
using Shortly.Domain.LinkManagement;

namespace Shortly.Application.Links;

public sealed class RedirectService
{
    private readonly ILinkRepository _linkRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IRedirectCache _redirectCache;

    public RedirectService(
        ILinkRepository linkRepository,
        IEventPublisher eventPublisher,
        IRedirectCache redirectCache)
    {
        _linkRepository = linkRepository;
        _eventPublisher = eventPublisher;
        _redirectCache = redirectCache;
    }

    public async Task<RedirectResult> ResolveAsync(
        string domainPrefix,
        string shortCode,
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cachedUrl = await _redirectCache.GetDestinationUrlAsync(domainPrefix, shortCode, cancellationToken);
        if (cachedUrl is not null)
        {
            _ = _eventPublisher.PublishFireAndForgetAsync(
                new LinkRedirectedEvent(
                    default,
                    new DomainPrefix(domainPrefix),
                    new ShortCode(shortCode)));

            return RedirectResult.Success(cachedUrl);
        }

        // Cache miss — fall through to database
        var link = await _linkRepository.GetByPrefixAndCodeAsync(
            new DomainPrefix(domainPrefix),
            new ShortCode(shortCode),
            cancellationToken);

        if (link is null)
            return RedirectResult.NotFound();

        if (!link.IsRedirectable())
            return RedirectResult.Gone();

        // Populate cache for next request
        await _redirectCache.SetDestinationUrlAsync(
            domainPrefix, shortCode, link.DestinationUrl.Value, cancellationToken);

        // Fire and forget — redirect must not block on event publishing
        _ = _eventPublisher.PublishFireAndForgetAsync(
            new LinkRedirectedEvent(link.Id, link.DomainPrefix, link.ShortCode));

        return RedirectResult.Success(link.DestinationUrl.Value);
    }
}

public sealed class RedirectResult
{
    public bool IsFound { get; }
    public bool IsGone { get; }
    public string? DestinationUrl { get; }

    private RedirectResult(bool isFound, bool isGone, string? destinationUrl)
    {
        IsFound = isFound;
        IsGone = isGone;
        DestinationUrl = destinationUrl;
    }

    public static RedirectResult Success(string destinationUrl) => new(true, false, destinationUrl);
    public static RedirectResult NotFound() => new(false, false, null);
    public static RedirectResult Gone() => new(false, true, null);
}

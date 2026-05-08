using Shortly.Application.Interfaces;
using Shortly.Domain.LinkManagement;

namespace Shortly.Application.Links;

public sealed class RedirectService
{
    private readonly ILinkRepository _linkRepository;
    private readonly IEventPublisher _eventPublisher;

    public RedirectService(ILinkRepository linkRepository, IEventPublisher eventPublisher)
    {
        _linkRepository = linkRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<RedirectResult> ResolveAsync(
        string domainPrefix,
        string shortCode,
        CancellationToken cancellationToken = default)
    {
        var link = await _linkRepository.GetByPrefixAndCodeAsync(
            new DomainPrefix(domainPrefix),
            new ShortCode(shortCode),
            cancellationToken);

        if (link is null)
            return RedirectResult.NotFound();

        if (!link.IsRedirectable())
            return RedirectResult.Gone();

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

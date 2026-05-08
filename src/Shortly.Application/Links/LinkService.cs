using Shortly.Application.Common;
using Shortly.Application.Interfaces;
using Shortly.Domain.Common;
using Shortly.Domain.LinkManagement;

namespace Shortly.Application.Links;

public sealed class LinkService
{
    private readonly ILinkRepository _linkRepository;
    private readonly ICustomDomainRepository _domainRepository;
    private readonly IShortCodeGenerator _codeGenerator;
    private readonly IEventPublisher _eventPublisher;

    public LinkService(
        ILinkRepository linkRepository,
        ICustomDomainRepository domainRepository,
        IShortCodeGenerator codeGenerator,
        IEventPublisher eventPublisher)
    {
        _linkRepository = linkRepository;
        _domainRepository = domainRepository;
        _codeGenerator = codeGenerator;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<Link>> CreateAsync(
        string domainPrefix,
        string destinationUrl,
        string createdBy,
        DateTimeOffset? expiresAt = null,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var prefix = new DomainPrefix(domainPrefix);

        if (!await _domainRepository.ExistsAsync(prefix, cancellationToken))
            return Result<Link>.Failure($"Domain prefix '{domainPrefix}' is not registered.");

        var domain = await _domainRepository.GetByPrefixAsync(prefix, cancellationToken);
        if (domain is not null && !domain.IsActive)
            return Result<Link>.Failure($"Domain prefix '{domainPrefix}' is deactivated.");

        var shortCode = await _codeGenerator.GenerateAsync(prefix, cancellationToken);

        var link = Link.Create(
            shortCode,
            prefix,
            new DestinationUrl(destinationUrl),
            createdBy,
            expiresAt,
            tags is not null ? new LinkMetadata(tags) : null);

        await _linkRepository.AddAsync(link, cancellationToken);
        await PublishEventsAsync(link);

        return Result<Link>.Success(link);
    }

    public async Task<Result<Link>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var link = await _linkRepository.GetByIdAsync(new LinkId(id), cancellationToken);
        return link is not null
            ? Result<Link>.Success(link)
            : Result<Link>.NotFound("Link not found.");
    }

    public async Task<Result<Link>> GetByPrefixAndCodeAsync(
        string domainPrefix,
        string shortCode,
        CancellationToken cancellationToken = default)
    {
        var link = await _linkRepository.GetByPrefixAndCodeAsync(
            new DomainPrefix(domainPrefix),
            new ShortCode(shortCode),
            cancellationToken);

        return link is not null
            ? Result<Link>.Success(link)
            : Result<Link>.NotFound("Link not found.");
    }

    public async Task<Result<Link>> DisableAsync(
        string domainPrefix,
        string shortCode,
        CancellationToken cancellationToken = default)
    {
        var link = await _linkRepository.GetByPrefixAndCodeAsync(
            new DomainPrefix(domainPrefix),
            new ShortCode(shortCode),
            cancellationToken);

        if (link is null)
            return Result<Link>.NotFound("Link not found.");

        link.Disable();
        await _linkRepository.UpdateAsync(link, cancellationToken);
        await PublishEventsAsync(link);

        return Result<Link>.Success(link);
    }

    private async Task PublishEventsAsync(Link link)
    {
        foreach (var domainEvent in link.DomainEvents)
        {
            await _eventPublisher.PublishAsync(domainEvent);
        }
        link.ClearDomainEvents();
    }
}

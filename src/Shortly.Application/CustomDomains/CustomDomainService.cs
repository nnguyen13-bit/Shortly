using Shortly.Application.Common;
using Shortly.Application.Interfaces;
using Shortly.Domain.CustomDomains;
using Shortly.Domain.LinkManagement;

namespace Shortly.Application.CustomDomains;

public sealed class CustomDomainService
{
    private readonly ICustomDomainRepository _repository;
    private readonly ILinkRepository _linkRepository;
    private readonly IEventPublisher _eventPublisher;

    public CustomDomainService(
        ICustomDomainRepository repository,
        ILinkRepository linkRepository,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _linkRepository = linkRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<CustomDomain>> RegisterAsync(
        string prefix,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var domainPrefix = new DomainPrefix(prefix);

        if (await _repository.ExistsAsync(domainPrefix, cancellationToken))
            return Result<CustomDomain>.Conflict($"Domain prefix '{prefix}' is already registered.");

        var domain = CustomDomain.Register(domainPrefix, name, description);

        await _repository.AddAsync(domain, cancellationToken);
        await PublishEventsAsync(domain);

        return Result<CustomDomain>.Success(domain);
    }

    public async Task<IReadOnlyList<CustomDomain>> ListAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(activeOnly, cancellationToken);
    }

    public async Task<Result<DeactivationResult>> DeactivateAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        var domainPrefix = new DomainPrefix(prefix);
        var domain = await _repository.GetByPrefixAsync(domainPrefix, cancellationToken);

        if (domain is null)
            return Result<DeactivationResult>.NotFound($"Domain prefix '{prefix}' not found.");

        var activeLinkCount = await _linkRepository.CountByPrefixAsync(domainPrefix, cancellationToken);

        domain.Deactivate();
        await _repository.UpdateAsync(domain, cancellationToken);
        await PublishEventsAsync(domain);

        string? warning = activeLinkCount > 0
            ? $"Warning: {activeLinkCount} active link(s) exist under this prefix."
            : null;

        return Result<DeactivationResult>.Success(new DeactivationResult(domain, warning));
    }

    private async Task PublishEventsAsync(CustomDomain domain)
    {
        foreach (var domainEvent in domain.DomainEvents)
        {
            await _eventPublisher.PublishAsync(domainEvent);
        }
        domain.ClearDomainEvents();
    }
}

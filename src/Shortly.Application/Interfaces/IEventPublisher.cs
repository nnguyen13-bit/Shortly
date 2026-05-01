using Shortly.Domain.Common;

namespace Shortly.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishFireAndForgetAsync(DomainEvent domainEvent);
}

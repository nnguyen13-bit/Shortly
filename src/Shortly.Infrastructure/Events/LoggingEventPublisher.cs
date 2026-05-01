using Microsoft.Extensions.Logging;
using Shortly.Application.Interfaces;
using Shortly.Domain.Common;

namespace Shortly.Infrastructure.Events;

/// <summary>
/// Initial event publisher that logs events. 
/// Will be replaced with ServiceBusEventPublisher in Task 1.6.
/// </summary>
public sealed class LoggingEventPublisher : IEventPublisher
{
    private readonly ILogger<LoggingEventPublisher> _logger;

    public LoggingEventPublisher(ILogger<LoggingEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Domain event published: {EventType} ({EventId})",
            domainEvent.GetType().Name, domainEvent.EventId);
        return Task.CompletedTask;
    }

    public Task PublishFireAndForgetAsync(DomainEvent domainEvent)
    {
        _logger.LogInformation("Domain event published (fire-and-forget): {EventType} ({EventId})",
            domainEvent.GetType().Name, domainEvent.EventId);
        return Task.CompletedTask;
    }
}

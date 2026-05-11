using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Shortly.Application.Interfaces;
using Shortly.Domain.Common;
using Shortly.Infrastructure.Configuration;
using Shortly.Infrastructure.Resilience;

namespace Shortly.Infrastructure.Events;

public sealed class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusEventPublisher> _logger;
    private readonly ResiliencePipeline _pipeline;

    private static readonly JsonSerializerOptions SerialiserOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ServiceBusEventPublisher(
        IOptions<ServiceBusSettings> settings,
        ILogger<ServiceBusEventPublisher> logger,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        var config = settings.Value;

        if (string.IsNullOrWhiteSpace(config.ConnectionString))
            throw new ArgumentException("ServiceBus connection string is not configured.", nameof(settings));

        _client = new ServiceBusClient(config.ConnectionString);
        _sender = _client.CreateSender(config.TopicName);
        _logger = logger;
        _pipeline = pipelineProvider.GetPipeline(ResilienceKeys.ServiceBus);
    }

    // Internal constructor for testing with injected dependencies
    internal ServiceBusEventPublisher(ServiceBusSender sender, ILogger<ServiceBusEventPublisher> logger, ResiliencePipeline pipeline)
    {
        _client = null!;
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipeline = pipeline;
    }

    public async Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var message = CreateMessage(domainEvent);

        try
        {
            await _pipeline.ExecuteAsync(async ct =>
            {
                await _sender.SendMessageAsync(message, ct);
                _logger.LogInformation("Published domain event to Service Bus: {EventType} ({EventId})",
                    domainEvent.GetType().Name, domainEvent.EventId);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain event to Service Bus after all retries: {EventType} ({EventId})",
                domainEvent.GetType().Name, domainEvent.EventId);
            throw;
        }
    }

    public Task PublishFireAndForgetAsync(DomainEvent domainEvent)
    {
        var message = CreateMessage(domainEvent);

        _ = SendWithRetryAsync(message, domainEvent);
        return Task.CompletedTask;
    }

    private async Task SendWithRetryAsync(ServiceBusMessage message, DomainEvent domainEvent)
    {
        try
        {
            await _sender.SendMessageAsync(message);
            _logger.LogInformation("Published domain event (fire-and-forget) to Service Bus: {EventType} ({EventId})",
                domainEvent.GetType().Name, domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish fire-and-forget domain event to Service Bus: {EventType} ({EventId})",
                domainEvent.GetType().Name, domainEvent.EventId);
        }
    }

    internal static ServiceBusMessage CreateMessage(DomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType().Name;
        var body = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerialiserOptions);

        var message = new ServiceBusMessage(body)
        {
            MessageId = domainEvent.EventId.ToString(),
            Subject = eventType,
            ContentType = "application/json",
            ApplicationProperties =
            {
                ["EventType"] = eventType,
                ["OccurredAt"] = domainEvent.OccurredAt.ToString("O")
            }
        };

        return message;
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();

        if (_client is not null)
            await _client.DisposeAsync();
    }
}

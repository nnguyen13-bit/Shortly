using System.Text.Json;
using Shortly.Domain.Common;
using Shortly.Domain.LinkManagement;
using Shortly.Infrastructure.Events;

namespace Shortly.Infrastructure.Tests.Events;

public sealed class ServiceBusEventPublisherTests
{
    // --- CreateMessage Tests ---

    [Fact]
    public void CreateMessage_SetsMessageIdToEventId()
    {
        var linkId = new LinkId(Guid.NewGuid());
        var prefix = new DomainPrefix("app");
        var code = new ShortCode("aBcDe");
        var url = new DestinationUrl("https://example.com");
        var domainEvent = new LinkCreatedEvent(linkId, prefix, code, url);

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        Assert.Equal(domainEvent.EventId.ToString(), message.MessageId);
    }

    [Fact]
    public void CreateMessage_SetsSubjectToEventTypeName()
    {
        var domainEvent = CreateLinkCreatedEvent();

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        Assert.Equal("LinkCreatedEvent", message.Subject);
    }

    [Fact]
    public void CreateMessage_SetsContentTypeToJson()
    {
        var domainEvent = CreateLinkCreatedEvent();

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        Assert.Equal("application/json", message.ContentType);
    }

    [Fact]
    public void CreateMessage_SetsEventTypeApplicationProperty()
    {
        var domainEvent = CreateLinkCreatedEvent();

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        Assert.Equal("LinkCreatedEvent", message.ApplicationProperties["EventType"]);
    }

    [Fact]
    public void CreateMessage_SetsOccurredAtApplicationProperty()
    {
        var domainEvent = CreateLinkCreatedEvent();

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        Assert.True(message.ApplicationProperties.ContainsKey("OccurredAt"));
        var occurredAt = (string)message.ApplicationProperties["OccurredAt"];
        Assert.True(DateTimeOffset.TryParse(occurredAt, out _), "OccurredAt should be a valid ISO 8601 date");
    }

    [Fact]
    public void CreateMessage_BodyContainsSerializedEvent()
    {
        var linkId = new LinkId(Guid.NewGuid());
        var prefix = new DomainPrefix("app");
        var code = new ShortCode("aBcDe");
        var url = new DestinationUrl("https://example.com/page");
        var domainEvent = new LinkCreatedEvent(linkId, prefix, code, url);

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        var body = message.Body.ToString();
        Assert.Contains("linkId", body);
        Assert.Contains("domainPrefix", body);
        Assert.Contains("shortCode", body);
        Assert.Contains("destinationUrl", body);
    }

    [Fact]
    public void CreateMessage_BodyIsValidJson()
    {
        var domainEvent = CreateLinkCreatedEvent();

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        var body = message.Body.ToString();
        var jsonDoc = JsonDocument.Parse(body);
        Assert.NotNull(jsonDoc);
    }

    [Fact]
    public void CreateMessage_UsesCamelCaseNaming()
    {
        var domainEvent = CreateLinkCreatedEvent();

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        var body = message.Body.ToString();
        Assert.Contains("eventId", body);
        Assert.Contains("occurredAt", body);
    }

    // --- Different Event Types ---

    [Fact]
    public void CreateMessage_LinkDisabledEvent_SetsCorrectSubject()
    {
        var linkId = new LinkId(Guid.NewGuid());
        var prefix = new DomainPrefix("app");
        var code = new ShortCode("aBcDe");
        var domainEvent = new LinkDisabledEvent(linkId, prefix, code);

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        Assert.Equal("LinkDisabledEvent", message.Subject);
        Assert.Equal("LinkDisabledEvent", message.ApplicationProperties["EventType"]);
    }

    [Fact]
    public void CreateMessage_LinkRedirectedEvent_SetsCorrectSubject()
    {
        var linkId = new LinkId(Guid.NewGuid());
        var prefix = new DomainPrefix("app");
        var code = new ShortCode("aBcDe");
        var domainEvent = new LinkRedirectedEvent(linkId, prefix, code);

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        Assert.Equal("LinkRedirectedEvent", message.Subject);
    }

    [Fact]
    public void CreateMessage_CustomDomainRegisteredEvent_SetsCorrectSubject()
    {
        var id = new Shortly.Domain.CustomDomains.CustomDomainId(Guid.NewGuid());
        var prefix = new DomainPrefix("app");
        var domainEvent = new Shortly.Domain.CustomDomains.CustomDomainRegisteredEvent(id, prefix, "My App");

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        Assert.Equal("CustomDomainRegisteredEvent", message.Subject);
    }

    [Fact]
    public void CreateMessage_CustomDomainDeactivatedEvent_SetsCorrectSubject()
    {
        var id = new Shortly.Domain.CustomDomains.CustomDomainId(Guid.NewGuid());
        var prefix = new DomainPrefix("app");
        var domainEvent = new Shortly.Domain.CustomDomains.CustomDomainDeactivatedEvent(id, prefix);

        var message = ServiceBusEventPublisher.CreateMessage(domainEvent);

        Assert.Equal("CustomDomainDeactivatedEvent", message.Subject);
    }

    // --- Unique Message IDs ---

    [Fact]
    public void CreateMessage_DifferentEvents_ProduceDifferentMessageIds()
    {
        var event1 = CreateLinkCreatedEvent();
        var event2 = CreateLinkCreatedEvent();

        var message1 = ServiceBusEventPublisher.CreateMessage(event1);
        var message2 = ServiceBusEventPublisher.CreateMessage(event2);

        Assert.NotEqual(message1.MessageId, message2.MessageId);
    }

    // --- Helper ---

    private static LinkCreatedEvent CreateLinkCreatedEvent()
    {
        return new LinkCreatedEvent(
            new LinkId(Guid.NewGuid()),
            new DomainPrefix("app"),
            new ShortCode("aBcDe"),
            new DestinationUrl("https://example.com"));
    }
}

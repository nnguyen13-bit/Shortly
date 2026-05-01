namespace Shortly.Infrastructure.Configuration;

public sealed class ServiceBusSettings
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "shortly-events";
}

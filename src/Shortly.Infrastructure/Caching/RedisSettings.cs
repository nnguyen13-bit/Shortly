namespace Shortly.Infrastructure.Caching;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>Cache entry time-to-live in seconds.</summary>
    public int CacheTtlSeconds { get; set; } = 300;
}

namespace Shortly.Api.Authentication;

public sealed class ApiKeySettings
{
    public const string SectionName = "ApiKeys";

    public List<ApiKeyClient> Clients { get; set; } = [];

    public ApiKeyClient? FindClient(string apiKey)
    {
        return Clients.Find(c =>
            string.Equals(c.Key, apiKey, StringComparison.Ordinal));
    }
}

public sealed class ApiKeyClient
{
    public required string Name { get; set; }
    public required string Key { get; set; }
}

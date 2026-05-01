using Shortly.Domain.Common;

namespace Shortly.Domain.LinkManagement;

public sealed class LinkMetadata : ValueObject
{
    private const int MaxTags = 10;
    private const int MaxKeyLength = 50;
    private const int MaxValueLength = 200;

    public IReadOnlyDictionary<string, string> Tags { get; }

    public LinkMetadata(IDictionary<string, string>? tags = null)
    {
        if (tags is null || tags.Count == 0)
        {
            Tags = new Dictionary<string, string>();
            return;
        }

        if (tags.Count > MaxTags)
            throw new ArgumentException(
                $"Metadata cannot exceed {MaxTags} tags. Got: {tags.Count}",
                nameof(tags));

        foreach (var (key, value) in tags)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be empty.", nameof(tags));

            if (key.Length > MaxKeyLength)
                throw new ArgumentException(
                    $"Metadata key must not exceed {MaxKeyLength} characters. Key '{key}' has {key.Length}.",
                    nameof(tags));

            if (value is not null && value.Length > MaxValueLength)
                throw new ArgumentException(
                    $"Metadata value must not exceed {MaxValueLength} characters. Key '{key}' value has {value.Length}.",
                    nameof(tags));
        }

        Tags = new Dictionary<string, string>(tags);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var tag in Tags.OrderBy(t => t.Key))
        {
            yield return tag.Key;
            yield return tag.Value;
        }
    }
}

using Shortly.Domain.Common;

namespace Shortly.Domain.LinkManagement;

public sealed class DestinationUrl : ValueObject
{
    private static readonly string[] ForbiddenSchemes = ["javascript", "data"];
    private const int MaxLength = 2048;

    public string Value { get; }

    public DestinationUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Destination URL cannot be empty.", nameof(value));

        if (value.Length > MaxLength)
            throw new ArgumentException(
                $"Destination URL must not exceed {MaxLength} characters. Got: {value.Length}",
                nameof(value));

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            throw new ArgumentException(
                $"Destination URL must be a valid absolute URI. Got: '{value}'",
                nameof(value));

        if (uri.Scheme is not ("http" or "https"))
            throw new ArgumentException(
                $"Destination URL must use HTTP or HTTPS scheme. Got: '{uri.Scheme}'",
                nameof(value));

        foreach (var scheme in ForbiddenSchemes)
        {
            if (value.StartsWith($"{scheme}:", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Destination URL must not use '{scheme}:' scheme.",
                    nameof(value));
        }

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

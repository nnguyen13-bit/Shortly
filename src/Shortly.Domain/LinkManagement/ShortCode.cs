using System.Text.RegularExpressions;
using Shortly.Domain.Common;

namespace Shortly.Domain.LinkManagement;

public sealed partial class ShortCode : ValueObject
{
    public string Value { get; }

    public ShortCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Short code cannot be empty.", nameof(value));

        if (!ShortCodeRegex().IsMatch(value))
            throw new ArgumentException(
                $"Short code must be 5-8 Base62 characters [a-zA-Z0-9]. Got: '{value}'",
                nameof(value));

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-zA-Z0-9]{5,8}$")]
    private static partial Regex ShortCodeRegex();
}

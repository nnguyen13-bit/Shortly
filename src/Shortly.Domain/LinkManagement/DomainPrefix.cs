using System.Text.RegularExpressions;
using Shortly.Domain.Common;

namespace Shortly.Domain.LinkManagement;

public sealed partial class DomainPrefix : ValueObject
{
    public string Value { get; }

    public DomainPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Domain prefix cannot be empty.", nameof(value));

        if (!DomainPrefixRegex().IsMatch(value))
            throw new ArgumentException(
                $"Domain prefix must be 2-4 lowercase alphanumeric characters [a-z0-9]. Got: '{value}'",
                nameof(value));

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z0-9]{2,4}$")]
    private static partial Regex DomainPrefixRegex();
}
